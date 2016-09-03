using System.Collections.Generic;

namespace TwitchIRC.Common
{
    /// <summary>
    /// Twitch irc message types
    /// </summary>
    public enum MessageType
    {
        UnknownCommand, //421
        ChatterJoinedChannel, //JOIN
        ChatterLeftChannel, //PART
        ChatMessage, //PRIVMSG
        ChatterNameListing, //353
        ChatterNameListingComplete, //366
        ChatterModeUpdate, //MODE
        ChannelNotice, //NOTICE
        HostNotice, //HOSTTARGET
        ChatCleared, //CLEARCHAT
        ChatterStatus, //USERSTATE
        Reconnect, //RECONNECT
        ChannelStatus, //ROOMSTATE
        GlobalChatterStatus, //GLOBALUSERSTATE
    }

    /// <summary>
    /// Twitch irc message
    /// </summary>
    public class Message
    {
        public MessageType Type { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        public string Prefix { get; set; }
        public string Destination { get; set; }
        public string Payload { get; set; }
    }
}
