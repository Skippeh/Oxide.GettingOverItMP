using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nancy;
using Nancy.ModelBinding;
using Newtonsoft.Json;
using WebAPI.Models;

namespace WebAPI.Modules
{
    public sealed class VersionModule : NancyModule
    {
        class UploadVersionDTO
        {
            [JsonRequired]
            public string Version { get; set; }

            [JsonRequired]
            public DateTime ReleaseDate { get; set; }
        }

        public VersionModule() : base("/version")
        {
            Get("/check", GetVersion);
            Post("/upload", UploadVersionAsync);
        }

        private async Task<object> GetVersion(dynamic args)
        {
            if (Data.LatestVersion != null)
                return await Response.AsJson(Data.LatestVersion);
            else
                return await Response.JsonError("No version was found.", HttpStatusCode.InternalServerError);
        }
        
        private async Task<dynamic> UploadVersionAsync(dynamic args, CancellationToken cancellationToken)
        {
            var file = Request.Files.FirstOrDefault();
            var data = JsonConvert.DeserializeObject<UploadVersionDTO>(Request.Form.data);
            
            if (file == null)
                return await Response.JsonError("No file found", HttpStatusCode.BadRequest);

            if (file.ContentType != "application/zip")
                return await Response.JsonError("File is not a zip file", HttpStatusCode.BadRequest);
            
            var modVersion = new ModVersion
            {
                Version = data.Version,
                ReleaseDate = data.ReleaseDate,
                DirectoryPath = $"versions/{data.Version}"
            };

            try
            {
                var zipArchive = new ZipArchive(file.Value, ZipArchiveMode.Read, false);

                // Calculate checksums
                using (var md5 = MD5.Create())
                {
                    foreach (ZipArchiveEntry entry in zipArchive.Entries)
                    {
                        // Ignore directories
                        if (entry.Name == string.Empty)
                            continue;

                        using (var entryStream = entry.Open())
                        {
                            var md5Bytes = md5.ComputeHash(entryStream);
                            string md5String = BitConverter.ToString(md5Bytes).Replace("-", "").ToLowerInvariant();
                            modVersion.Checksums.Add(new ModVersion.FileChecksum {FilePath = entry.FullName, Md5 = md5String});
                        }
                    }
                }
            }
            catch (InvalidDataException)
            {
                return await Response.JsonError("File is not a valid zip file.", HttpStatusCode.BadRequest);
            }

            Directory.CreateDirectory($"versions/{data.Version}");

            // Save archive
            using (var fileStream = File.Create($"versions/{data.Version}/archive.zip"))
            {
                file.Value.Position = 0;
                byte[] bytes = new byte[file.Value.Length];
                file.Value.Read(bytes, 0, bytes.Length);
                fileStream.Write(bytes, 0, bytes.Length);
            }

            // Save json file
            string json = JsonConvert.SerializeObject(modVersion);
            File.WriteAllText($"{modVersion.DirectoryPath}/version.json", json, Encoding.UTF8);

            // Add version to version index if it's not already there.
            if (!Data.VersionIndex.Contains(modVersion.Version))
                Data.VersionIndex.Add(modVersion.Version);

            Data.Versions.Add(modVersion);
            Data.InvalidateLatestVersion();
            await Data.SaveAsync(default);
            return await Response.AsJson(modVersion);
        }
    }
}
