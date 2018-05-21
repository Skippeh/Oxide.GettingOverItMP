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
using Nancy.Security;
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
            Get("/{type}", (args, token) => GetLatestVersionAsync(args, false));
            Get("/{type}/all", (args, token) => GetLatestVersionAsync(args, true));
            Post("/{type}/upload", UploadVersionAsync);
            Get("/{type}/{version}/file/{filePath*}", DownloadFileAsync);
            Get("/{type}/{version}/archive", (args, token) => DownloadArchiveAsync(args, false));
            Get("/{type}/{version}/archive/all", (args, token) => DownloadArchiveAsync(args, true));
            Get("/{type}/history", (args, token) => GetVersionHistoryAsync(args, false));
            Get("/{type}/history/all", (args, token) => GetVersionHistoryAsync(args, true));
        }
        
        private async Task<object> GetLatestVersionAsync(dynamic args, bool includeUnreleased)
        {
            if (includeUnreleased)
                this.RequiresAuthentication();

            if (!ParseModType(args, out ModType type, out Response errorResponse))
                return await errorResponse;

            if (Data.GetLatestVersion(type, includeUnreleased, out var modVersion))
                return await Response.AsJson(modVersion);
            else
                return await Response.JsonError("No version was found.", HttpStatusCode.InternalServerError);
        }

        private async Task<object> GetVersionHistoryAsync(dynamic args, bool includeUnreleased)
        {
            if (includeUnreleased)
                this.RequiresAuthentication();

            if (!ParseModType(args, out ModType modType, out Response errorResponse))
                return await errorResponse;

            var now = DateTime.UtcNow;
            return await Response.AsJson(Data.Versions.Where(v => v.Type == modType && (includeUnreleased || v.ReleaseDate <= now)).OrderByDescending(v => v.ReleaseDate).Select(v => new
            {
                v.Version,
                v.ReleaseDate,
                v.Type
            }));
        }

        private async Task<object> DownloadFileAsync(dynamic args)
        {
            if (!ParseModType(args, out ModType modType, out Response errorResponse))
                return await errorResponse;

            string versionQuery = args.version;
            ModVersion version = Data.FindVersion(modType, versionQuery, includeUnreleased: false);

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

        private async Task<object> DownloadArchiveAsync(dynamic args, bool includeUnreleased)
        {
            if (includeUnreleased)
                this.RequiresAuthentication();

            if (!ParseModType(args, out ModType modType, out Response errorResponse))
                return await errorResponse;

            string versionQuery = args.version;
            ModVersion version = Data.FindVersion(modType, versionQuery, includeUnreleased);

            if (version == null)
                return await Response.Error($"The version '{versionQuery}' could not be found.", HttpStatusCode.NotFound);

            var fileStream = new FileStream(Path.Combine(version.DirectoryPath, "archive.zip"), FileMode.Open, FileAccess.Read);
            var response = new StreamResponse(() => fileStream, MimeTypes.GetMimeType("archive.zip"));
            return await response.AsAttachment($"goimp-{modType.ToString().ToLowerInvariant()}-{version.Version}.zip");
        }

        private async Task<object> UploadVersionAsync(dynamic args, CancellationToken cancellationToken)
        {
            this.RequiresAuthentication();

            if (!ParseModType(args, out ModType modType, out Response errorResponse))
                return await errorResponse;

            var file = Request.Files.FirstOrDefault();
            UploadVersionDTO data = JsonConvert.DeserializeObject<UploadVersionDTO>(Request.Form.data);
            
            if (file == null)
                return await Response.JsonError("No file found", HttpStatusCode.BadRequest);

            if (file.ContentType != "application/zip" &&
                file.ContentType != "application/x-zip-compressed" &&
                file.ContentType != "application/zip-compressed")
                return await Response.JsonError("File is not a zip file", HttpStatusCode.BadRequest);

            // Verify that the version not older or equal to the current latest version
            if (Data.GetLatestVersion(modType, true, out ModVersion latestVersion))
            {
                if (modType == ModType.Server)
                {
                    if (!Version.TryParse(data.Version, out Version serverVersion))
                    {
                        return await Response.JsonError("The specified version is not valid.", HttpStatusCode.BadRequest);
                    }

                    var currentServerVersion = Version.Parse(latestVersion.Version);

                    /*if (serverVersion <= currentServerVersion)
                    {
                        return await Response.JsonError("The specified version is older or equal to the current version.", HttpStatusCode.BadRequest);
                    }*/
                }
                else if (modType == ModType.Client)
                {
                    if (!data.Version.Contains("_"))
                        return await Response.JsonError("The specified version is not valid.", HttpStatusCode.BadRequest);

                    string[] currentVersions = latestVersion.Version.Split('_');
                    Version currentClientVersion = Version.Parse(currentVersions[0]);
                    Version currentGameVersion = Version.Parse(currentVersions[1]);
                    string[] versions = data.Version.Split('_');

                    if (!Version.TryParse(versions[0], out Version clientVersion) || !Version.TryParse(versions[1], out Version gameVersion))
                        return await Response.JsonError("The specified version is not valid.", HttpStatusCode.BadRequest);

                    /*if (clientVersion <= currentClientVersion && gameVersion <= currentGameVersion)
                        return await Response.JsonError("The specified version is older or equal to the current version.", HttpStatusCode.BadRequest);*/
                }
            }
            
            var modVersion = new ModVersion
            {
                Version = data.Version,
                ReleaseDate = data.ReleaseDate,
                DirectoryPath = $"versions/{modType}/{data.Version}",
                Type = modType
            };

            byte[] archiveBytes = new byte[file.Value.Length];
            await file.Value.ReadAsync(archiveBytes, 0, archiveBytes.Length, cancellationToken);
            var zipStream = new MemoryStream();
            await zipStream.WriteAsync(archiveBytes, 0, archiveBytes.Length, cancellationToken);
            zipStream.Position = 0;

            try
            {
                using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Update, true))
                {
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

                    // Add version.json to archive.
                    var versionEntry = zipArchive.CreateEntry("version.json", CompressionLevel.Optimal);
                    using (var stream = versionEntry.Open())
                    {
                        string versionJson = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(modVersion), cancellationToken);
                        byte[] bytes = Encoding.UTF8.GetBytes(versionJson);
                        await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
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
                zipStream.Position = 0;
                byte[] bytes = new byte[zipStream.Length];
                await zipStream.ReadAsync(bytes, 0, bytes.Length, cancellationToken);
                await fileStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            }

            zipStream.Dispose();

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
            string strType = ((string) args.type)?.ToLowerInvariant();

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
