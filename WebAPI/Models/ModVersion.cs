using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
        public ModType Type;

        /// <summary>The path to the directory that contains the version.json and archive file.</summary>
        [JsonIgnore] public string DirectoryPath;

        /// <summary>
        /// Opens the archive in readonly mode.
        /// </summary>
        /// <returns></returns>
        public ZipArchive OpenZipArchive()
        {
            return ZipFile.Open(Path.Combine(DirectoryPath, "archive.zip"), ZipArchiveMode.Read);
        }
    }

    public enum ModType
    {
        Invalid,
        Client,
        Server
    }
}
