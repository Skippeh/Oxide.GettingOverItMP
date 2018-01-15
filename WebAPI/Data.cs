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

        public static List<Tuple<ModType, string>> VersionIndex { get; private set; } = new List<Tuple<ModType, string>>();
        public static List<ModVersion> Versions { get; private set; } = new List<ModVersion>();

        private static ModVersion latestClientVersion;
        public static ModVersion LatestClientVersion => latestClientVersion ?? (latestClientVersion = Versions.Where(v => v.ReleaseDate <= DateTime.UtcNow && v.Type == ModType.Client).OrderByDescending(v => v.ReleaseDate).FirstOrDefault());

        private static ModVersion latestServerVersion;
        public static ModVersion LatestServerVersion => latestServerVersion ?? (latestServerVersion = Versions.Where(v => v.ReleaseDate <= DateTime.UtcNow && v.Type == ModType.Server).OrderByDescending(v => v.ReleaseDate).FirstOrDefault());

        public static List<UserCredential> UserCredentials { get; private set; } = new List<UserCredential>
        {
            new UserCredential("changeme", "CHANGEME")
        };

        public static ModVersion GetLatestVersion(ModType type)
        {
            return type == ModType.Client ? LatestClientVersion : LatestServerVersion;
        }

        public static bool GetLatestVersion(ModType type, out ModVersion modVersion)
        {
            modVersion = GetLatestVersion(type);
            return modVersion != null;
        }

        public static void Load()
        {
            InvalidateLatestVersion();

            if (File.Exists("versions/index.json"))
            {
                string indexJson = File.ReadAllText("versions/index.json", Encoding.UTF8);
                VersionIndex = JsonConvert.DeserializeObject<List<Tuple<ModType, string>>>(indexJson, SerializerSettings);

                foreach (var t in VersionIndex)
                {
                    string directoryPath = $"versions/{t.Item1}/{t.Item2}";
                    string json = File.ReadAllText(Path.Combine(directoryPath, "version.json"), Encoding.UTF8);
                    ModVersion modVersion = JsonConvert.DeserializeObject<ModVersion>(json, SerializerSettings);
                    modVersion.DirectoryPath = directoryPath;
                    Versions.Add(modVersion);
                }
            }

            if (File.Exists("credentials.json"))
            {
                string json = File.ReadAllText("credentials.json", Encoding.UTF8);
                UserCredentials = JsonConvert.DeserializeObject<List<UserCredential>>(json);
            }
            else
            {
                string json = JsonConvert.SerializeObject(UserCredentials, Formatting.Indented, SerializerSettings);
                File.WriteAllText("credentials.json", json, Encoding.UTF8);
            }

            foreach (var user in UserCredentials.ToList())
            {
                if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password))
                {
                    UserCredentials.Remove(user);
                    Console.WriteLine($"Refused user account: {user.Username} (username or password empty)");
                    continue;
                }

                Console.WriteLine($"Added user account: {user.Username}");
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
            latestClientVersion = null;
            latestServerVersion = null;
        }

        public static ModVersion FindVersion(ModType modType, string version)
        {
            if (version == "latest")
                return GetLatestVersion(modType);

            return Versions.FirstOrDefault(v => v.Type == modType && v.Version.ToLowerInvariant() == version.ToLowerInvariant());
        }

        public static bool FindVersion(ModType modType, string version, out ModVersion modVersion)
        {
            modVersion = FindVersion(modType, version);
            return modVersion != null;
        }
    }

    public class UserCredential
    {
        public string Username;
        public string Password;

        [JsonConstructor]
        private UserCredential() { }

        public UserCredential(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}
