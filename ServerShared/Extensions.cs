using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using ServerShared.Player;
using UnityEngine;

namespace ServerShared
{
    public static class Extensions
    {
        public static void Put(this NetDataWriter writer, MessageType messageType)
        {
            writer.Put((byte) messageType);
        }

        public static void Put(this NetDataWriter writer, ServerShared.DisconnectReason reason)
        {
            writer.Put((byte) reason);
        }

        public static void Put(this NetDataWriter writer, Vector3 vec3)
        {
            writer.Put(vec3.x);
            writer.Put(vec3.y);
            writer.Put(vec3.z);
        }

        public static Vector3 GetVector3(this NetDataReader reader)
        {
            return new Vector3(reader.GetFloat(),
                               reader.GetFloat(),
                               reader.GetFloat());
        }

        public static void Put(this NetDataWriter writer, Quaternion quat)
        {
            writer.Put(quat.x);
            writer.Put(quat.y);
            writer.Put(quat.z);
            writer.Put(quat.w);
        }

        public static Quaternion GetQuaternion(this NetDataReader reader)
        {
            return new Quaternion(reader.GetFloat(),
                                  reader.GetFloat(),
                                  reader.GetFloat(),
                                  reader.GetFloat());
        }

        public static void Put(this NetDataWriter writer, IDictionary<int, PlayerMove> moves)
        {
            writer.Put(moves.Count);

            foreach (var kv in moves)
            {
                var move = kv.Value;
                
                writer.Put(kv.Key); // The player ID
                writer.Put(move);
            }
        }

        public static Dictionary<int, PlayerMove> GetMovementDictionary(this NetDataReader reader)
        {
            var result = new Dictionary<int, PlayerMove>();

            int count = reader.GetInt();

            for (int i = 0; i < count; ++i)
            {
                result.Add(reader.GetInt(), reader.GetPlayerMove());
            }

            return result;
        }

        public static void Put(this NetDataWriter writer, IDictionary<int, string> names)
        {
            writer.Put(names.Count);

            foreach (var kv in names)
            {
                string name = kv.Value;

                writer.Put(kv.Key); // The player ID
                writer.Put(name);
            }
        }

        public static Dictionary<int, string> GetNamesDictionary(this NetDataReader reader)
        {
            var result = new Dictionary<int, string>();

            int count = reader.GetInt();

            for (int i = 0; i < count; ++i)
            {
                result.Add(reader.GetInt(), reader.GetString());
            }

            return result;
        }

        public static void Put(this NetDataWriter writer, PlayerMove move)
        {
            writer.Put(move.Position);
            writer.Put(move.Rotation);

            writer.Put(move.AnimationAngle);
            writer.Put(move.AnimationExtension);

            writer.Put(move.HandlePosition);
            writer.Put(move.HandleRotation);

            writer.Put(move.SliderPosition);
            writer.Put(move.SliderRotation);
        }

        public static PlayerMove GetPlayerMove(this NetDataReader reader)
        {
            return new PlayerMove
            {
                Position = reader.GetVector3(),
                Rotation = reader.GetQuaternion(),

                AnimationAngle = reader.GetFloat(),
                AnimationExtension = reader.GetFloat(),

                HandlePosition = reader.GetVector3(),
                HandleRotation = reader.GetQuaternion(),

                SliderPosition = reader.GetVector3(),
                SliderRotation = reader.GetQuaternion()
            };
        }
    }
}
