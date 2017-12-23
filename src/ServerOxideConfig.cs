using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Oxide.Core;
using ServerShared;

namespace Oxide.GettingOverItMP
{
    public class ServerOxideConfig : IServerConfig
    {
        private const int binaryVersion = 0;

        public bool LoadPlayerBans(out List<PlayerBan> bans)
        {
            string filePath = GetFilePath("goimp/bans.bin");

            if (!File.Exists(filePath))
            {
                bans = new List<PlayerBan>();
                return SavePlayerBans(bans);
            }

            using (var reader = File.OpenRead(filePath))
            {
                byte[] bytes = new byte[reader.Length];
                reader.Read(bytes, 0, bytes.Length);
                bans = Deserialize(bytes);
                return true;
            }
        }

        public bool SavePlayerBans(IEnumerable<PlayerBan> bans)
        {
            byte[] bytes = Serialize(bans);

            using (var writer = File.Create(GetFilePath("goimp/bans.bin")))
            {
                writer.Write(bytes, 0, bytes.Length);
            }

            return true;
        }

        private string GetFilePath(string fileName)
        {
            string fullPath = Path.Combine(Interface.Oxide.ConfigDirectory, fileName);
            string directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            return Path.Combine(Interface.Oxide.ConfigDirectory, fileName);
        }

        private byte[] Serialize(IEnumerable<PlayerBan> bansEnumerable)
        {
            var bans = bansEnumerable.ToList();

            using (var memstream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memstream, Encoding.UTF8))
                {
                    writer.Write(binaryVersion);
                    writer.Write(bans.Count);

                    foreach (var ban in bans)
                    {
                        writer.Write((byte) ban.Type);

                        if (ban.Type == PlayerBan.BanType.Ip)
                            writer.Write(ban.Ip);
                        else if (ban.Type == PlayerBan.BanType.SteamId)
                            writer.Write(ban.SteamId);

                        writer.Write(ban.Reason != null);

                        if (ban.Reason != null)
                            writer.Write(ban.Reason);

                        writer.Write(ban.ExpirationDate != null);

                        if (ban.ExpirationDate != null)
                            writer.Write(ban.ExpirationDate.Value.Ticks);
                    }
                }

                memstream.Flush();
                return memstream.ToArray();
            }
        }

        private List<PlayerBan> Deserialize(byte[] bytes)
        {
            var result = new List<PlayerBan>();

            using (var memstream = new MemoryStream(bytes))
            {
                var reader = new BinaryReader(memstream, Encoding.UTF8);
                int version = reader.ReadInt32();
                var count = reader.ReadInt32();

                for (int i = 0; i < count; ++i)
                {
                    var ban = new PlayerBan();
                    ban.Type = (PlayerBan.BanType) reader.ReadByte();

                    if (ban.Type == PlayerBan.BanType.Ip)
                        ban.Ip = reader.ReadUInt32();
                    else if (ban.Type == PlayerBan.BanType.SteamId)
                        ban.SteamId = reader.ReadUInt64();

                    if (reader.ReadBoolean())
                        ban.Reason = reader.ReadString();

                    if (reader.ReadBoolean())
                        ban.ExpirationDate = new DateTime(reader.ReadInt64());

                    result.Add(ban);
                }
            }

            return result;
        }
    }
}
