using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using ServerShared.Logging;

namespace ServerShared
{
    public static class MasterServer
    {
        public static GameServer Server { get; set; }

        private static DateTime nextBeat;
        private static int failedAttempts = 0;
        private static Exception lastException;
        private static bool started;
        private static Thread updateThread;
        private static WebClient webClient;

        /// <summary>Max retries before giving up until the next heartbeat.</summary>
        private const int MaxRetries = 2;

        /// <summary>How often to send heartbeat to master server in minutes.</summary>
        private const int BeatInterval = 1;

        /// <summary>How often to retry sending heartbeat to master server in minutes.</summary>
        private const double FailBeatInterval = 5 / 60d; // 5 seconds

        public static void Start(GameServer server)
        {
            if (started)
                throw new Exception("Start was called while already running.");

            started = true;
            Server = server ?? throw new ArgumentNullException(nameof(server));
            webClient = new WebClient();
            updateThread = new Thread(DoUpdateThread);
            updateThread.Start();

            Logger.LogDebug("Started beating to master server.");
        }

        public static void Stop()
        {
            webClient.Dispose();
            webClient = null;
            started = false;
        }

        private static void DoUpdateThread()
        {
            while (started)
            {
                if (DateTime.UtcNow >= nextBeat)
                {
                    if (Beat(Server.Port))
                    {
                        nextBeat = DateTime.UtcNow.AddMinutes(BeatInterval);
                        failedAttempts = 0;
                    }
                    else
                    {
                        if (failedAttempts >= MaxRetries)
                        {
                            // Give up this time, try again at the next beat interval.
                            nextBeat = DateTime.UtcNow.AddMinutes(BeatInterval);
                            failedAttempts = 0;
                            Logger.LogException("Failed to send heartbeat to master server.", lastException);
                        }
                        else
                        {
                            // Try again after a shorter amount of time.
                            nextBeat = DateTime.UtcNow.AddMinutes(FailBeatInterval);
                            failedAttempts++;
                        }
                    }
                }

                Thread.Sleep(100);
            }
        }

        private static bool Beat(int port)
        {
            try
            {
                byte[] byteResponse = webClient.UploadValues($"{SharedConstants.MasterServerUrl}/beat", "POST", new NameValueCollection
                {
                    {"port", port.ToString()},
                    {"version", SharedConstants.Version.ToString() }
                });

                string json = Encoding.UTF8.GetString(byteResponse);

                if (!webClient.ResponseHeaders["Content-Type"].StartsWith("application/json"))
                    return true;

                JObject response = JObject.Parse(json);
                string status = response["status"].ToObject<string>();
                string message = response["message"].ToObject<string>();

                if (status == "warning")
                {
                    Logger.LogWarning($"MasterServer responded with a warning: {message}");
                }
                else if (status == "error")
                {
                    Logger.LogError($"MasterServer responded with an error: {message}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                lastException = ex;
                return false;
            }
        }
    }
}
