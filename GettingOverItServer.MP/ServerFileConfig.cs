using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ServerShared;

namespace GettingOverItMP.Server
{
    public class ServerFileConfig : IServerConfig
    {
        public readonly string Directory;

        private static readonly JsonSerializerSettings serializerSettings;

        static ServerFileConfig()
        {
            serializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
        }

        public ServerFileConfig(string directory)
        {
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            Directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, directory);

            Console.WriteLine($"Config directory set to: {Directory}");
            CheckDirectory();
        }

        public bool LoadPlayerBans(out List<PlayerBan> bans)
        {
            if (!File.Exists(GetFilePath("bans.json")))
            {
                bans = new List<PlayerBan>();
                return SavePlayerBans(bans);
            }

            try
            {
                string json = File.ReadAllText(GetFilePath("bans.json"));
                bans = JsonConvert.DeserializeObject<List<PlayerBan>>(json) ?? new List<PlayerBan>();
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is JsonSerializationException)
            {
                Console.WriteLine($"Failed to load player bans: {ex.Message}");
                bans = null;
                return false;
            }
        }

        public bool SavePlayerBans(IEnumerable<PlayerBan> bans)
        {
            CheckDirectory();

            try
            {
                using (var writer = File.CreateText(GetFilePath("bans.json")))
                {
                    string json = JsonConvert.SerializeObject(bans.ToList(), serializerSettings);
                    writer.Write(json);
                }

                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is JsonSerializationException)
            {
                Console.WriteLine($"Failed to save player bans: {ex.Message}");
                return false;
            }
        }

        private void CheckDirectory()
        {
            if (!System.IO.Directory.Exists(Directory))
                System.IO.Directory.CreateDirectory(Directory);
        }

        private string GetFilePath(string fileName)
        {
            return Path.Combine(Directory, fileName);
        }
    }
}
