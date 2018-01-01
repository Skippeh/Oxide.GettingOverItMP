using System;

namespace ServerShared.Logging
{
    public static class Logger
    {
        public static event LogMessageReceivedEventHandler LogMessageReceived;

        public static void LogInfo(object msg)
        {
            Log(msg, LogMessageType.Info);
        }

        public static void LogDebug(object msg)
        {
            Log(msg, LogMessageType.Debug);
        }

        public static void LogWarning(object msg)
        {
            Log(msg, LogMessageType.Warning);
        }

        public static void LogError(object msg)
        {
            Log(msg, LogMessageType.Error);
        }

        public static void LogException(object msg, Exception ex)
        {
            Log(msg, LogMessageType.Exception, ex);
        }

        public static void LogException(Exception ex)
        {
            Log(null, LogMessageType.Info, ex);
        }

        public static void Log(object msg, LogMessageType type, Exception ex = null)
        {
            switch (type)
            {
                case LogMessageType.Info:
                    LogMessageReceived?.Invoke(new LogMessageReceivedEventArgs(LogMessageType.Info, msg));
                    break;
                case LogMessageType.Debug:
                    LogMessageReceived?.Invoke(new LogMessageReceivedEventArgs(LogMessageType.Debug, msg));
                    break;
                case LogMessageType.Warning:
                    LogMessageReceived?.Invoke(new LogMessageReceivedEventArgs(LogMessageType.Warning, msg));
                    break;
                case LogMessageType.Error:
                    LogMessageReceived?.Invoke(new LogMessageReceivedEventArgs(LogMessageType.Error, msg));
                    break;
                case LogMessageType.Exception:
                    LogMessageReceived?.Invoke(new LogMessageReceivedEventArgs(LogMessageType.Exception, msg ?? string.Empty, ex));
                    break;
            }
        }
    }

    public delegate void LogMessageReceivedEventHandler(LogMessageReceivedEventArgs args);
}
