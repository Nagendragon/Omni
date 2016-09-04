using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchIRC.Common;

namespace TwitchIRC.Interfaces
{
    public interface ITwitchIRCClient
    {
        bool IsRunning { get; }
        RetryPolicy RetryPolicy { get; set; }
        IDictionary<string, ITwitchIRCChannel> Channels { get; }
        ITwitchIRCChannel AggregateChannel { get; }
        string Username { get; }

        event EventHandler<RawMessageEventArgs> OnRawMessage;
        event EventHandler<MessageEventArgs> OnMessage;
        event EventHandler<ConnectEventArgs> OnConnect;
        event EventHandler<ConnectionExceptionEventArgs> OnConnectionException;

        Task Start();
        Task Stop();

        Task<ITwitchIRCChannel> JoinChannel(string name);
        Task LeaveChannel(ITwitchIRCChannel channel);
        Task SendRawMessage(string message);
    }
}
