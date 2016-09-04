using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchIRC.Common;

namespace TwitchIRC.Interfaces
{
    public interface ITwitchIRCChannel
    {
        string Name { get; }
        IDictionary<string, ITwitchIRCChatter> Chatters { get; }

        event EventHandler<RawMessageEventArgs> OnRawMessage;
        event EventHandler<MessageEventArgs> OnMessage;

        Task InvokeMessage(MessageEventArgs e);
        Task InvokeRawMessage(RawMessageEventArgs e);

        Task SendToChat(string payload);
        Task Leave();
    }
}
