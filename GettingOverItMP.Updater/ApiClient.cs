using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GettingOverItMP.Updater.Exceptions;
using GettingOverItMP.Updater.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GettingOverItMP.Updater
{
    public class ApiClient : IDisposable
    {
        public const string ApiUrl = "https://api.gettingoverit.mp";

        public readonly WebClient WebClient;

        public ApiClient()
        {
            WebClient = new WebClient();
        }

        public void Dispose()
        {
            WebClient?.Dispose();
        }

        public ModVersion QueryLatestVersion(ModType modType)
        {
            try
            {
                return QueryLatestVersionAsync(modType).Result;
            }
            catch (AggregateException aex)
            {
                if (aex.InnerException != null)
                    throw aex.InnerException;

                throw aex.GetBaseException();
            }
        }

        public async Task<ModVersion> QueryLatestVersionAsync(ModType modType)
        {
            try
            {
                string responseJson = await WebClient.DownloadStringTaskAsync($"{ApiUrl}/version/{modType.ToString().ToLowerInvariant()}");
                ModVersion modVersion = JsonConvert.DeserializeObject<ModVersion>(responseJson);
                return modVersion;
            }
            catch (WebException ex)
            {
                var httpWebResponse = ex.Response as HttpWebResponse;

                if (httpWebResponse == null)
                    throw;

                if (httpWebResponse.StatusCode != HttpStatusCode.BadRequest)
                    throw;

                using (var responseStream = httpWebResponse.GetResponseStream())
                {
                    if (responseStream == null)
                        throw;

                    byte[] responseBytes = new byte[responseStream.Length];
                    string responseJson = Encoding.UTF8.GetString(responseBytes);
                    dynamic response = JObject.Parse(responseJson);

                    if (response.error != null)
                    {
                        string errorMessage = response.error;
                        throw new ApiRequestFailedException(errorMessage, ex);
                    }

                    throw new ApiRequestFailedException("Failed to query the api. No error message was given.", ex);
                }
            }
        }

        public async Task DownloadFileAsync(string filePath, string targetFilePath, ModType modType, string version, Action<DownloadProgressChangedEventArgs> onProgress = null)
        {
            string directory = Path.GetDirectoryName(targetFilePath);

            if (directory != string.Empty && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            void ProgressChanged(object sender, DownloadProgressChangedEventArgs args)
            {
                onProgress?.Invoke(args);
            }

            WebClient.DownloadProgressChanged += ProgressChanged;

            string address = $"{ApiUrl}/version/{modType.ToString().ToLowerInvariant()}/{version}/file/{filePath}";
            await WebClient.DownloadFileTaskAsync(address, targetFilePath);

            WebClient.DownloadProgressChanged -= ProgressChanged;
        }
    }
}
