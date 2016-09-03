using System;

namespace TwitchIRC.Common
{
    public delegate void RawMessageEventHandler(object sender, RawMessageEventArgs e);

    public delegate void MessageEventHandler(object sender, MessageEventArgs e);

    public delegate void ConnectEventHandler(object sender, ConnectEventArgs e);

    public class ConnectEventArgs : EventArgs
    {
        public bool Connected { get; private set; }
        public ConnectEventArgs(bool connected) : base()
        {
            Connected = connected;
        }
    }

    public class RawMessageEventArgs : EventArgs
    {
        public string Content { get; private set; }
        public RawMessageEventArgs(string message) : base()
        {
            Content = message;
        }
    }

    public class MessageEventArgs : EventArgs
    {
        public Message Message { get; private set; }
        public MessageEventArgs(Message message) : base()
        {
            Message = message;
        }
    }
}
