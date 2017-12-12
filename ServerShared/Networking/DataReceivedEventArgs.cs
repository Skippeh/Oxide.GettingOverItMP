using System;
using Lidgren.Network;

namespace ServerShared
{
    public class DataReceivedEventArgs : EventArgs
    {
        public NetConnection Connection => Message.SenderConnection;
        public NetIncomingMessage Message { get; set; }
        public MessageType MessageType { get; set; }
    }
}
