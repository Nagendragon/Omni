using System;

namespace TwitchIRC.Common
{
    /// <summary>
    /// Capability flags for connecting to twitch irc service
    /// </summary>
    [Flags]
    public enum TwitchIRCClientCapabilities
    {
        NONE =          0,
        Membership =    1 << 0,
        Commands =      1 << 1,
        Tags =          1 << 2
    }
}
