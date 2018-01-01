using System;

namespace ServerShared.Logging
{
    public static class Logger
    {
        public static event LogMessageReceivedEventHandler LogMessageReceived;

        public static void LogInfo(object msg)
        {
            LogMessageReceived?.Invoke(new LogMessageReceivedEventArgs(LogMessageType.Info, msg));
        }

        public static void LogDebug(object msg)
        {
            LogMessageReceived?.Invoke(new LogMessageReceivedEventArgs(LogMessageType.Debug, msg));
        }

        public static void LogWarning(object msg)
        {
            LogMessageReceived?.Invoke(new LogMessageReceivedEventArgs(LogMessageType.Warning, msg));
        }

        public static void LogError(object msg)
        {
            LogMessageReceived?.Invoke(new LogMessageReceivedEventArgs(LogMessageType.Error, msg));
        }

        public static void LogException(object msg, Exception ex)
        {
            LogMessageReceived?.Invoke(new LogMessageReceivedEventArgs(LogMessageType.Exception, msg, ex));
        }

        public static void LogException(Exception ex)
        {
            LogMessageReceived?.Invoke(new LogMessageReceivedEventArgs(LogMessageType.Exception, string.Empty, ex));
        }
    }

    public delegate void LogMessageReceivedEventHandler(LogMessageReceivedEventArgs args);
}
