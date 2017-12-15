using System;
using System.Collections.Specialized;
using System.Net;
using System.Threading;

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

            Server = server ?? throw new ArgumentNullException(nameof(server));
            webClient = new WebClient();
            updateThread = new Thread(DoUpdateThread);
            updateThread.Start();
            started = true;

            Console.WriteLine("Started beating to master server.");
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
                            Console.WriteLine($"Failed to send heartbeat to master server: {lastException.Message}");
                        }
                        else
                        {
                            // Try again after a shorter amount of time.
                            nextBeat = DateTime.UtcNow.AddMinutes(FailBeatInterval);
                            failedAttempts++;
                        }
                    }

                    Thread.Sleep(100);
                }
            }
        }

        private static bool Beat(int port)
        {
            try
            {
                webClient.UploadValues($"{SharedConstants.MasterServerUrl}/beat", "POST", new NameValueCollection
                {
                    {"port", port.ToString()}
                });

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
