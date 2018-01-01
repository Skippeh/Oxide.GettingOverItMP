using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ServerShared.Logging;
using ServerShared.Player;

namespace ServerShared
{
    public class ServerConfig
    {
        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        [JsonIgnore]
        public string Directory { get; private set; }

        public List<PlayerBan> Bans { get; private set; } = new List<PlayerBan>();
        public List<PlayerAccessLevelIdentity> AccessLevels { get; private set; } = new List<PlayerAccessLevelIdentity>();
        
        private ServerConfig(string directory)
        {
            Directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        /// <summary>Do not use. Used for deserialization.</summary>
        [JsonConstructor]
        private ServerConfig() { }

        public bool Save()
        {
            return SaveConfig(Directory, this);
        }

        public static bool LoadConfig(string directory, out ServerConfig config)
        {
            CheckDirectory(directory);

            string savePath = Path.Combine(directory, "config.json");

            Logger.LogDebug($"Loading config from {savePath}");

            if (!File.Exists(savePath))
            {
                config = new ServerConfig(directory);
                bool saveSuccessful = config.Save();

                if (saveSuccessful)
                    Logger.LogWarning($"Created new config at {savePath}");
                else
                    Logger.LogError($"Failed to create new config at {savePath}");

                return saveSuccessful;
            }

            try
            {
                string json = File.ReadAllText(savePath);
                config = JsonConvert.DeserializeObject<ServerConfig>(json, serializerSettings);
                config.Directory = directory;

                // Remove expired bans and save config.
                if (config.RemoveExpiredBans() > 0)
                    config.Save();

                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException)
            {
                Logger.LogException("Failed to load player bans.", ex);
                config = null;
                return false;
            }
        }

        public static bool SaveConfig(string directory, ServerConfig config)
        {
            CheckDirectory(directory);

            try
            {
                using (var writer = File.CreateText(Path.Combine(directory, "config.json")))
                {
                    string json = JsonConvert.SerializeObject(config, serializerSettings);
                    writer.Write(json);
                }

                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException || ex is Exception)
            {
                Logger.LogException("Failed to save player bans.", ex);
                return false;
            }
        }

        private static void CheckDirectory(string directory)
        {
            if (!System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);
        }

        /// <summary>Removes all expired bans and returns the amount of bans removed.</summary>
        public int RemoveExpiredBans()
        {
            return Bans.RemoveAll(ban => ban.Expired());
        }
    }
}
