using System;
using Lidgren.Network;

namespace ServerShared
{
    public class DisconnectedEventArgs : EventArgs
    {
        public NetConnection Connection { get; set; }
        public DisconnectReason Reason { get; set; }
        public string ReasonString { get; set; }
        public string AdditionalInfo { get; set; }
    }
}
