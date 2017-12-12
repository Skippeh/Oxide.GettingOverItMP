using System;
using Lidgren.Network;

namespace ServerShared
{
    public static class StaticGamePeer
    {
        public static void Update<T>(T peer) where T : NetPeer, IGamePeer
        {
            while (peer.ReadMessage(out NetIncomingMessage message))
            {
                switch (message.MessageType)
                {
                    case NetIncomingMessageType.StatusChanged:
                        var status = (NetConnectionStatus) message.ReadByte();
                        string reason = message.ReadString();

                        DisconnectReason? enumReason = null;
                        int enumReasonInt;

                        if (int.TryParse(reason, out enumReasonInt))
                        {
                            enumReason = (DisconnectReason) enumReasonInt;
                        }

                        if (status == NetConnectionStatus.Connected)
                        {
                            peer.InvokeConnected(peer, new ConnectedEventArgs {Connection = message.SenderConnection});
                        }
                        else if (status == NetConnectionStatus.Disconnected)
                        {
                            Console.WriteLine($"{message.SenderEndPoint} new status: {status} ({reason})");
                            peer.InvokeDisconnected(peer, new DisconnectedEventArgs {Connection = message.SenderConnection, Reason = enumReason ?? DisconnectReason.Invalid, ReasonString = reason});
                        }

                        break;
                    case NetIncomingMessageType.Data:
                        if (message.Data.Length == 0)
                        {
                            message.SenderConnection.Disconnect(DisconnectReason.InvalidMessage);
                            return;
                        }

                        peer.InvokeDataReceived(peer, new DataReceivedEventArgs {Message = message, MessageType = (MessageType) message.ReadByte()});

                        break;
                }
            }
        }
    }
}
