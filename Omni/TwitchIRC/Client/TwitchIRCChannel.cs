using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchIRC.Common;
using TwitchIRC.Interfaces;
using TwitchIRC.Utility_Primitives;

namespace TwitchIRC.Client
{
    public class TwitchIRCChannel : ITwitchIRCChannel
    {
        private ITwitchIRCClient _parent;

        public TwitchIRCChannel(string name, ITwitchIRCClient parent)
        {
            Name = name;
            _parent = parent;
        }

        public IDictionary<string, ITwitchIRCChatter> Chatters
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string Name{ get; private set; }

        public event EventHandler<MessageEventArgs> OnMessage = delegate { };
        public event EventHandler<RawMessageEventArgs> OnRawMessage = delegate { };

        public async Task InvokeMessage(MessageEventArgs e)
        {
            await OnMessage.InvokeAsync(this, e);
        }

        public async Task InvokeRawMessage(RawMessageEventArgs e)
        {
            await OnRawMessage.InvokeAsync(this, e);
        }

        public async Task SendToChat(string payload)
        {
            await _parent.SendRawMessage(String.Format(":{0}!{0}@{0}.tmi.twitch.tv PRIVMSG #{1} :{2}", _parent.Username, Name, payload));
        }

        public async Task Leave()
        {
            await _parent.LeaveChannel(this);
        }
    }
}
