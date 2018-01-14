using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GettingOverItMP.Updater.Models
{
    public class ModVersion
    {
        public class FileChecksum
        {
            public string FilePath;
            public string Md5;

            public override bool Equals(object obj)
            {
                if (!(obj is FileChecksum))
                    return false;

                var checksum = obj as FileChecksum;
                return string.Equals(FilePath, checksum.FilePath) && string.Equals(Md5, checksum.Md5);
            }

            /// <summary>Returns true if the local file has a matching md5 hash.</summary>
            public bool MatchesExistingFile() => Utility.GetFileHash(FilePath) == Md5;
        }

        public string Version;
        public DateTime ReleaseDate;
        public List<FileChecksum> Checksums = new List<FileChecksum>();
        public ModType Type;

        [JsonIgnore]
        public Version ServerVersion => System.Version.Parse(Version);

        /// <summary>
        /// Returns the mod and game version.
        /// </summary>
        [JsonIgnore]
        public Tuple<Version, Version> ClientVersion
        {
            get
            {
                string[] versions = Version.Split('_');
                var result = new Tuple<Version, Version>(System.Version.Parse(versions[0]), System.Version.Parse(versions[1]));
                return result;
            }
        }
    }

    public enum ModType
    {
        Invalid,
        Client,
        Server
    }
}
