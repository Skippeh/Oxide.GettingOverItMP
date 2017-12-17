using System.Collections.Generic;
using Lidgren.Network;
using ServerShared.Networking;
using ServerShared.Player;

namespace ServerShared
{
    public static class Extensions
    {
        public static void Write(this NetOutgoingMessage message, MessageType messageType)
        {
            message.Write((byte) messageType);
        }

        public static void Write(this NetOutgoingMessage message, ServerShared.DisconnectReason reason)
        {
            message.Write((byte) reason);
        }
        
        public static void Write(this NetOutgoingMessage message, IDictionary<int, PlayerMove> moves)
        {
            message.Write(moves.Count);

            foreach (var kv in moves)
            {
                var move = kv.Value;
                
                message.Write(kv.Key); // The player ID
                message.Write(move);
            }
        }

        public static Dictionary<int, PlayerMove> ReadMovementDictionary(this NetIncomingMessage message)
        {
            var result = new Dictionary<int, PlayerMove>();

            int count = message.ReadInt32();

            for (int i = 0; i < count; ++i)
            {
                result.Add(message.ReadInt32(), message.ReadPlayerMove());
            }

            return result;
        }

        public static void Write(this NetOutgoingMessage message, IDictionary<int, string> names)
        {
            message.Write(names.Count);

            foreach (var kv in names)
            {
                string name = kv.Value;

                message.Write(kv.Key); // The player ID
                message.Write(name);
            }
        }

        public static Dictionary<int, string> ReadNamesDictionary(this NetIncomingMessage message)
        {
            var result = new Dictionary<int, string>();

            int count = message.ReadInt32();

            for (int i = 0; i < count; ++i)
            {
                result.Add(message.ReadInt32(), message.ReadString());
            }

            return result;
        }

        public static void Write(this NetOutgoingMessage message, PlayerMove move)
        {
            message.Write(move.Position);
            message.Write(move.Rotation);

            message.Write(move.AnimationAngle);
            message.Write(move.AnimationExtension);

            message.Write(move.HandlePosition);
            message.Write(move.HandleRotation);

            message.Write(move.SliderPosition);
            message.Write(move.SliderRotation);
        }

        public static PlayerMove ReadPlayerMove(this NetIncomingMessage message)
        {
            return new PlayerMove
            {
                Position = message.ReadVector3(),
                Rotation = message.ReadQuaternion(),

                AnimationAngle = message.ReadSingle(),
                AnimationExtension = message.ReadSingle(),

                HandlePosition = message.ReadVector3(),
                HandleRotation = message.ReadQuaternion(),

                SliderPosition = message.ReadVector3(),
                SliderRotation = message.ReadQuaternion()
            };
        }

        public static void Write(this NetOutgoingMessage message, DiscoveryServerInfo serverInfo)
        {
            message.Write(serverInfo.Name);
            message.Write(serverInfo.Players);
            message.Write(serverInfo.MaxPlayers);
        }

        public static DiscoveryServerInfo ReadDiscoveryServerInfo(this NetIncomingMessage message)
        {
            var info = new DiscoveryServerInfo
            {
                Name = message.ReadString(),
                Players = message.ReadUInt16(),
                MaxPlayers = message.ReadUInt16()
            };

            if (info.Name.Length > SharedConstants.MaxServerNameLength)
                info.Name = info.Name.Substring(0, SharedConstants.MaxServerNameLength);

            return info;
        }

        public static void Disconnect(this NetConnection connection, DisconnectReason reason)
        {
            connection.Disconnect(((byte) reason).ToString());
        }
    }
}
