using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchIRC.Common;

namespace TwitchIRC.Interfaces
{
    public interface ITwitchIRCChannel
    {
        string Name { get; }
        IDictionary<string, ITwitchIRCChatter> Chatters { get; }

        event RawMessageEventHandler OnRawMessage;
        event MessageEventHandler OnMessage;

        Task SendToChat(string payload);
    }
}
