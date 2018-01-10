using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebAPI.Models;

namespace WebAPI
{
    public static class Data
    {
        public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public static List<string> VersionIndex { get; private set; } = new List<string>();
        public static List<ModVersion> Versions { get; private set; } = new List<ModVersion>();

        private static ModVersion latestVersion;
        public static ModVersion LatestVersion => latestVersion ?? (latestVersion = Versions.Where(v => v.ReleaseDate <= DateTime.UtcNow).OrderByDescending(v => v.ReleaseDate).FirstOrDefault());
        
        public static void Load()
        {
            InvalidateLatestVersion();

            if (File.Exists("versions/index.json"))
            {
                string indexJson = File.ReadAllText("versions/index.json", Encoding.UTF8);
                VersionIndex = JsonConvert.DeserializeObject<List<string>>(indexJson, SerializerSettings);

                foreach (string version in VersionIndex)
                {
                    string filePath = $"versions/{version}/version.json";
                    string json = File.ReadAllText(filePath, Encoding.UTF8);
                    ModVersion modVersion = JsonConvert.DeserializeObject<ModVersion>(json, SerializerSettings);
                    modVersion.DirectoryPath = Path.GetDirectoryName(filePath);
                    Versions.Add(modVersion);
                }
            }
        }

        public static void Save()
        {
            Task.WaitAll(SaveAsync(default));
        }

        public static async Task SaveAsync(CancellationToken cancellationToken)
        {
            string indexJson = JsonConvert.SerializeObject(VersionIndex, SerializerSettings);
            await File.WriteAllTextAsync("versions/index.json", indexJson, Encoding.UTF8, cancellationToken);

            foreach (ModVersion version in Versions)
            {
                string json = JsonConvert.SerializeObject(version, SerializerSettings);
                await File.WriteAllTextAsync($"{version.DirectoryPath}/version.json", json, Encoding.UTF8, cancellationToken);
            }
        }

        public static void InvalidateLatestVersion()
        {
            latestVersion = null;
        }
    }
}
