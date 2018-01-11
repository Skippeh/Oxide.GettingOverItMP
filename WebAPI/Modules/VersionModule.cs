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
using Nancy.Responses;
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
            Get("/{type}", GetVersion);
            Post("/{type}/upload", UploadVersionAsync);
            Get("/{type}/{version}/file/{filePath*}", DownloadFileAsync);
        }

        private async Task<object> GetVersion(dynamic args)
        {
            if (!ParseModType(args, out ModType type, out Response errorResponse))
                return await errorResponse;

            if (Data.GetLatestVersion(type, out var modVersion))
                return await Response.AsJson(modVersion);
            else
                return await Response.JsonError("No version was found.", HttpStatusCode.InternalServerError);
        }

        private async Task<Response> DownloadFileAsync(dynamic args)
        {
            if (!ParseModType(args, out ModType modType, out Response errorResponse))
                return await errorResponse;

            string versionQuery = args.version;
            ModVersion version = Data.FindVersion(modType, versionQuery);

            if (version == null)
                return await Response.JsonError($"The version '{versionQuery}' could not be found.", HttpStatusCode.NotFound);

            string filePath = args.filePath;
            ZipArchive archive = version.OpenZipArchive();

            foreach (var entry in archive.Entries)
            {
                if (entry.Name == string.Empty)
                    continue;

                if (entry.FullName.ToLowerInvariant() == filePath.ToLowerInvariant())
                {
                    Stream fileStream = entry.Open();
                    var response = new ZipStreamResponse(archive, () => fileStream, MimeTypes.GetMimeType(entry.FullName));
                    return await response
                                         .WithHeader("X-File-Length", entry.Length.ToString())
                                         .AsAttachment(entry.Name);
                }
            }

            archive.Dispose();

            return await Response.JsonError("The file could not be found.", HttpStatusCode.NotFound);
        }
        
        private async Task<Response> UploadVersionAsync(dynamic args, CancellationToken cancellationToken)
        {
            if (!ParseModType(args, out ModType modType, out Response errorResponse))
                return await errorResponse;

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
                DirectoryPath = $"versions/{modType}/{data.Version}",
                Type = modType
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

            Directory.CreateDirectory(modVersion.DirectoryPath);

            // Save archive
            using (var fileStream = File.Create(Path.Combine(modVersion.DirectoryPath, "archive.zip")))
            {
                file.Value.Position = 0;
                byte[] bytes = new byte[file.Value.Length];
                await file.Value.ReadAsync(bytes, 0, bytes.Length, cancellationToken);
                await fileStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            }

            // Save json file
            string json = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(modVersion), cancellationToken);
            File.WriteAllText(Path.Combine(modVersion.DirectoryPath, "version.json"), json, Encoding.UTF8);

            // Add version to version index if it's not already there.
            var indexTuple = new Tuple<ModType, string>(modType, modVersion.Version);
            if (!Data.VersionIndex.Contains(indexTuple))
                Data.VersionIndex.Add(indexTuple);

            Data.Versions.Add(modVersion);
            Data.InvalidateLatestVersion();
            await Data.SaveAsync(default);
            return await Response.AsJson(modVersion);
        }

        private bool ParseModType(dynamic args, out ModType type, out Response response)
        {
            string strType = args.type;

            if (strType == "client")
                type = ModType.Client;
            else if (strType == "server")
                type = ModType.Server;
            else
            {
                type = ModType.Invalid;
                response = Response.JsonError("Invalid mod type specified.", HttpStatusCode.BadRequest);
                return false;
            }

            response = null;
            return true;
        }
    }
}
