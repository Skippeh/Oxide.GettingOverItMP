using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ServerShared
{
    /// <summary>
    /// Provides a wrapper for quering the web api using WebClient.
    /// </summary>
    public class ApiClient : IDisposable
    {
        public enum ModType
        {
            Invalid,
            Client,
            Server
        }

        private class ModVersion
        {
            public string Version;
        }

        private class ErrorResponse
        {
            public string Error;
        }

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

        public string QueryLatestVersion(ModType modType)
        {
            try
            {
                string responseJson = WebClient.DownloadString($"{ApiUrl}/version/{modType.ToString().ToLowerInvariant()}");
                var modVersion = JsonConvert.DeserializeObject<ModVersion>(responseJson);
                return modVersion.Version;
            }
            catch (WebException ex)
            {
                var httpWebResponse = ex.Response as HttpWebResponse;

                if (httpWebResponse == null)
                    throw new ApiRequestFailedException("Failed to query the api: " + ex.Message, ex);

                if (httpWebResponse.StatusCode != HttpStatusCode.BadRequest)
                    throw new ApiRequestFailedException("Failed to query the api: " + ex.Message, ex);

                using (var responseStream = httpWebResponse.GetResponseStream())
                {
                    if (responseStream == null)
                        throw;

                    byte[] responseBytes = new byte[responseStream.Length];
                    string responseJson = Encoding.UTF8.GetString(responseBytes);
                    ErrorResponse errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseJson);

                    if (errorResponse.Error != null)
                    {
                        throw new ApiRequestFailedException(errorResponse.Error, ex);
                    }

                    throw new ApiRequestFailedException("Failed to query the api: " + ex.Message, ex);
                }
            }
            catch (Exception ex)
            {
                throw new ApiRequestFailedException("Failed to query the api: " + ex.Message, ex);
            }
        }
    }

    public class ApiRequestFailedException : Exception
    {
        public ApiRequestFailedException(string errorMessage, Exception innerException) : base(errorMessage, innerException)
        {
        }
    }
}
