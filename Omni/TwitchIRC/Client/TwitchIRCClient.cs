using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using TwitchIRC.Common;
using TwitchIRC.Interfaces;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using TwitchIRC.Utility_Primitives;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace TwitchIRC.Client
{
    public class TwitchIRCClient : ITwitchIRCClient
    {
        //task management
        private volatile bool _finishedConnecting = false;
        private volatile AsyncManualResetEvent _stopToken = new AsyncManualResetEvent();
        private readonly SemaphoreSlim m_Connection = new SemaphoreSlim(1,1);
        Task _runner;

        //data source
        private TcpClient _tcpClient;
        private TextReader _inputStream;
        private TextWriter _outputStream;

        //config
        string _hostname;
        int _port;
        string _oAuth;
        string _username;
        TwitchIRCClientCapabilities _capabilities;

        //channels
        ConcurrentDictionary<string, ITwitchIRCChannel> _channels = new ConcurrentDictionary<string, ITwitchIRCChannel>();

        public TwitchIRCClient(string hostname, int port, string oAuth, string username
            , TwitchIRCClientCapabilities capabilities = TwitchIRCClientCapabilities.Commands | TwitchIRCClientCapabilities.Membership | TwitchIRCClientCapabilities.Tags)
        {
            _hostname = hostname;
            _port = port;
            _oAuth = oAuth;
            _username = username;
            _capabilities = capabilities;

            OnRawMessage += _ParseMessage;

            _runner = _ReadMessages();
        }

        public string Username { get { return _username; } }

        public ITwitchIRCChannel AggregateChannel
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IDictionary<string, ITwitchIRCChannel> Channels
        {
            get
            {
                return _channels;
            }
        }

        public bool IsRunning
        {
            get
            {
                return _stopToken.WaitAsync().IsCompleted;
            }
        }

        public RetryPolicy RetryPolicy { get; set; }

        public event EventHandler<ConnectEventArgs> OnConnect = delegate { };
        public event EventHandler<MessageEventArgs> OnMessage = delegate { };
        public event EventHandler<RawMessageEventArgs> OnRawMessage = delegate { };
        public event EventHandler<ConnectionExceptionEventArgs> OnConnectionException = delegate { };

        public async Task<ITwitchIRCChannel> JoinChannel(string name)
        {
            if (await _WaitForConnection())
            {
                await _outputStream.WriteLineAsync(String.Format("JOIN #{0}", name));
                await _outputStream.FlushAsync();
                return _channels.GetOrAdd(name, new TwitchIRCChannel(name, this));
            }
            else
            {
                TimeoutException ex = new TimeoutException("Could not join channel because the operation timed out before a valid connection was found.");
                ex.Data.Add("channel", name);
                await OnConnectionException.InvokeAsync(this, new ConnectionExceptionEventArgs(ex));
            }
            return null;
        }

        public async Task LeaveChannel(ITwitchIRCChannel channel)
        {
            if (await _WaitForConnection())
            {
                ITwitchIRCChannel removed;
                if (_channels.TryRemove(channel.Name, out removed))
                {
                    await _outputStream.WriteLineAsync(String.Format("PART #{0}", channel.Name));
                    await _outputStream.FlushAsync();
                }
            }
            else
            {
                TimeoutException ex = new TimeoutException("Could not leave channel because the operation timed out before a valid connection was found.");
                ex.Data.Add("channel", channel);
                await OnConnectionException.InvokeAsync(this, new ConnectionExceptionEventArgs(ex));
            }
        }

        public async Task Start()
        {
            await m_Connection.WaitAsync().ConfigureAwait(false);
            try
            {
                _stopToken.Set();
            }finally
            {
                m_Connection.Release();
            }
        }

        public async Task Stop()
        {
            await m_Connection.WaitAsync().ConfigureAwait(false);
            try
            {
                _stopToken.Reset();

                if (_tcpClient?.Connected ?? false)
                {
                    _tcpClient.Close();
                }
            }
            finally
            {
                m_Connection.Release();
            }
            await _InvokeConnect(new ConnectEventArgs(false));
        }

        public async Task SendRawMessage(string message)
        {
            if (await _WaitForConnection())
            {
                await _outputStream.WriteLineAsync(message);
                await _outputStream.FlushAsync();
            }else
            {
                TimeoutException ex = new TimeoutException("Could not send message because the operation timed out before a valid connection was found.");
                ex.Data.Add("message", message);
                await OnConnectionException.InvokeAsync(this, new ConnectionExceptionEventArgs(ex));
            }
        }

        #region Private Helpers
        private static MessageType _ParseType(string type)
        {
            switch (type)
            {
                case "421":
                    return MessageType.UnknownCommand;
                case "JOIN":
                    return MessageType.ChatterJoinedChannel;
                case "PART":
                    return MessageType.ChatterLeftChannel;
                case "PRIVMSG":
                    return MessageType.ChatMessage;
                case "353":
                    return MessageType.ChatterNameListing;
                case "366":
                    return MessageType.ChatterNameListingComplete;
                case "MODE":
                    return MessageType.ChatterModeUpdate;
                case "NOTICE":
                    return MessageType.ChannelNotice;
                case "HOSTTARGET":
                    return MessageType.HostNotice;
                case "CLEARCHAT":
                    return MessageType.ChatCleared;
                case "USERSTATE":
                    return MessageType.ChatterStatus;
                case "ROOMSTATE":
                    return MessageType.ChannelStatus;
                case "GLOBALUSERSTATE":
                    return MessageType.GlobalChatterStatus;
                case "RECONNECT":
                    return MessageType.Reconnect;
                default:
                    return MessageType.UnknownCommand;
            }
        }

        //default raw message handler
        private async void _ParseMessage(object sender, RawMessageEventArgs e)
        {
            string message = e.Content;
            Regex messageParser = new Regex(@"^(?:[@]([\S]+) )?(?:[:](\S+) )?(\S+)(?: (?!:)(.+?))?(?: [:](.+))?$");
            Regex tagParser = new Regex(@"(([^=]+)[=]([^;]*)[;]?)");
            try
            {
                var match = messageParser.Match(message);
                var tags = match.Groups[1].Value.Trim();
                var prefix = match.Groups[2].Value.Trim();
                var type = match.Groups[3].Value.Trim();
                var destination = match.Groups[4].Value.Trim();
                var payload = match.Groups[5].Value.Trim();

                var tagparsed = tagParser.Matches(tags);
                Dictionary<string, string> taglist = new Dictionary<string, string>();
                foreach (Match tag in tagparsed)
                {
                    taglist.Add(tag.Groups[2].Value, tag.Groups[3].Value);
                }

                Message m = new Message()
                {
                    Destination = destination,
                    Payload = payload,
                    Tags = taglist,
                    Prefix = prefix,
                    Type = _ParseType(type)
                };

                await _InvokeMessage(new MessageEventArgs(m));
            }
            catch
            {
                //Couldn't parse the message
            }
        }

        private async Task _Connect(string host, int port, string oAuth, string username, TwitchIRCClientCapabilities capabilities = TwitchIRCClientCapabilities.Commands | TwitchIRCClientCapabilities.Membership | TwitchIRCClientCapabilities.Tags)
        {
            _tcpClient = new TcpClient(host, port);
            _inputStream = TextReader.Synchronized(new StreamReader(_tcpClient.GetStream()));
            _outputStream = TextWriter.Synchronized(new StreamWriter(_tcpClient.GetStream()));

            await _outputStream.WriteLineAsync(String.Format("PASS oauth:{0}", oAuth));
            await _outputStream.WriteLineAsync(String.Format("NICK {0}", username));
            await _outputStream.FlushAsync();

            if (capabilities.HasFlag(TwitchIRCClientCapabilities.Membership))
            {
                await _outputStream.WriteLineAsync(String.Format("CAP REQ :twitch.tv/membership"));
            }

            if (capabilities.HasFlag(TwitchIRCClientCapabilities.Commands))
            {
                await _outputStream.WriteLineAsync(String.Format("CAP REQ :twitch.tv/commands"));
            }

            if (capabilities.HasFlag(TwitchIRCClientCapabilities.Tags))
            {
                await _outputStream.WriteLineAsync(String.Format("CAP REQ :twitch.tv/tags"));
            }

            await _outputStream.FlushAsync();
            //if this is a reconnect, we may need to rejoin channels that we lost.
            if(_channels.Count > 0)
            {
                foreach(var i in _channels)
                {
                    if (!string.IsNullOrWhiteSpace(i.Value?.Name))
                    {
                        await JoinChannel(i.Value.Name);
                    }
                }
            }
            await _outputStream.FlushAsync();

            _finishedConnecting = true;
            _InvokeConnect(new ConnectEventArgs(true));
        }

        private async Task _ReadMessages()
        {
            while (await _stopToken.WaitAsync())
            {
                await m_Connection.WaitAsync().ConfigureAwait(false);
                try
                {
                    //Check for running to eliminate race condition between start of iteration and semaphore wait.
                    if (IsRunning)
                    {
                        //check for connection and connect if needed.
                        if (!(_tcpClient?.Connected ?? false))
                        {
                            _finishedConnecting = false;
                            await _Connect(_hostname, _port, _oAuth, _username, _capabilities);
                        }
                        else//otherwise, get messages
                        {
                            string message = await _inputStream.ReadLineAsync();
                            if (!string.IsNullOrWhiteSpace(message))
                            {
                                //invoke any raw message handlers
                                await _InvokeRawMessage(new RawMessageEventArgs(message));
                            }
                            else { await Task.Delay(5); }
                        }
                    }
                }catch(Exception ex)
                {
                    try
                    {
                        await _InvokeConnectionException(new ConnectionExceptionEventArgs(ex));
                    }catch{ }
                }finally
                {
                    m_Connection.Release();
                }
            }
        }

        private Task<bool> _WaitForConnection(int timeout = 10000)
        {
            CancellationTokenSource cts = new CancellationTokenSource(10000);
            return Task.Run(async ()=> {
                while (!cts.Token.IsCancellationRequested)
                {
                    if(_finishedConnecting && (_tcpClient?.Connected ?? false))
                    {
                        return true;
                    }
                    await Task.Delay(5);
                }
                return false;
            });
        }

        private async Task _InvokeConnect(ConnectEventArgs e)
        {
            await OnConnect.InvokeAsync(this, e);
        }
        private async Task _InvokeMessage(MessageEventArgs e)
        {
            await OnMessage.InvokeAsync(this, e);
        }
        private async Task _InvokeRawMessage(RawMessageEventArgs e)
        {
            await OnRawMessage.InvokeAsync(this, e);
        }
        private async Task _InvokeConnectionException(ConnectionExceptionEventArgs e)
        {
            await OnConnectionException.InvokeAsync(this, e);
        }
        #endregion
    }
}
