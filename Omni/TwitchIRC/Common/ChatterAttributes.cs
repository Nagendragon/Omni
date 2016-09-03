using System;
using System.Drawing;

namespace TwitchIRC.Common
{
    /// <summary>
    /// Flags for twitch badges
    /// </summary>
    [Flags]
    public enum TwitchBadge
    {
        NONE =          0,
        staff =         1 << 0,
        admin =         1 << 1,
        global_mod =    1 << 2,
        moderator =     1 << 3,
        subscriber =    1 << 4,
        turbo =         1 << 5
    }

    /// <summary>
    /// Colors for twitch chat
    /// </summary>
    public struct TwitchColor
    {
        public TwitchColor(KnownColor knownColor)
        {
            Color = Color.FromKnownColor(knownColor);
        }

        public TwitchColor(int R, int G, int B)
        {
            Color = Color.FromArgb(255, R, G, B);
        }

        public Color Color { get; set; }

        string Hex {
            get {
                return Color.R.ToString("X2") 
                    + Color.G.ToString("X2") 
                    + Color.B.ToString("X2");
            }
        }
    }

    /// <summary>
    /// Bits/Cheers for twitch chatters
    /// </summary>
    public struct TwitchBits
    {
        int Amount { get; set; }

        TwitchColor Color
        {
            get
            {
                return Amount >= 10000 ? new TwitchColor(KnownColor.Red)
                    : Amount >= 5000 ? new TwitchColor(KnownColor.Blue)
                    : Amount >= 1000 ? new TwitchColor(KnownColor.Green)
                    : Amount >= 100 ? new TwitchColor(KnownColor.Purple)
                    : new TwitchColor(KnownColor.Gray);
            }
        }
    }
}
