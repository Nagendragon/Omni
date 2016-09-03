using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchIRC.Common;

namespace TwitchIRC.Interfaces
{
    public interface ITwitchIRCClient
    {
        bool IsConnected { get; }
        RetryPolicy RetryPolicy { get; set; }
        IDictionary<string, ITwitchIRCChannel> Channels { get; }
        ITwitchIRCChannel AggregateChannel { get; }

        event RawMessageEventHandler OnRawMessage;
        event MessageEventHandler OnMessage;
        event ConnectEventHandler OnConnect;

        Task Start();
        Task Stop();

        Task<ITwitchIRCChannel> JoinChannel(string name);
        Task LeaveChannel(ITwitchIRCChannel channel);
    }
}
