using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace WebAPI.Models
{
    public class ModVersion
    {
        public class FileChecksum
        {
            public string FilePath;
            public string Md5;
        }

        public string Version;
        public DateTime ReleaseDate;
        public List<FileChecksum> Checksums = new List<FileChecksum>();

        /// <summary>The path to the directory that contains the version.json and archive file.</summary>
        [JsonIgnore] public string DirectoryPath;
    }
}
