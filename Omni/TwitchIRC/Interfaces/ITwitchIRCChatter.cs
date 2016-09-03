using TwitchIRC.Common;

namespace TwitchIRC.Interfaces
{
    public interface ITwitchIRCChatter
    {
        string Name { get; set; }

        TwitchBadge Badges { get; set; }

        TwitchColor ChatColor { get; set; }

        TwitchBits Bits { get; set; }
    }
}
