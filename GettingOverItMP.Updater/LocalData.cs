using System;
using System.IO;
using GettingOverItMP.Updater.Models;
using Newtonsoft.Json;

namespace GettingOverItMP.Updater
{
    public static class LocalData
    {
        public static ModVersion Version { get; private set; }

        public static bool Load()
        {
            if (!File.Exists("version.json"))
            {
                Console.WriteLine("Local version file could not be found, downloading latest.");
                return true;
            }

            try
            {
                string json = File.ReadAllText("version.json");
                Version = JsonConvert.DeserializeObject<ModVersion>(json);
            }
            catch (Exception ex) when (ex is JsonException || ex is IOException)
            {
                Console.Error.WriteLine($"Failed to load local version: {ex}");
                return false;
            }

            return true;
        }
    }
}
