using System;
using Lidgren.Network;
using ServerShared.Logging;

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
                    case NetIncomingMessageType.DiscoveryRequest:
                        peer.InvokeDiscoveryRequest(peer, message);
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        var status = (NetConnectionStatus) message.ReadByte();
                        string fullReason = message.ReadString();
                        string reason = fullReason.Contains(";") ? fullReason.Substring(0, fullReason.IndexOf(";")) : fullReason;
                        string additionalInfo = fullReason.Contains(";") ? fullReason.Substring(fullReason.IndexOf(";") + 1) : null;

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
                            Logger.LogDebug($"{message.SenderEndPoint} new status: {status} ({enumReason?.ToString() ?? reason})" + $" {additionalInfo}");
                            peer.InvokeDisconnected(peer, new DisconnectedEventArgs {Connection = message.SenderConnection, Reason = enumReason ?? DisconnectReason.Invalid, ReasonString = reason, AdditionalInfo = additionalInfo});
                        }

                        break;
                    case NetIncomingMessageType.Data:
                        if (message.Data.Length == 0)
                        {
                            message.SenderConnection.Disconnect(DisconnectReason.InvalidMessage);
                            break;
                        }

                        peer.InvokeDataReceived(peer, new DataReceivedEventArgs {Message = message, MessageType = (MessageType) message.ReadByte()});

                        break;
                }
            }
        }
    }
}
