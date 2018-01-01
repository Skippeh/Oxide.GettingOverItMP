using System;

namespace ServerShared.Logging
{
    public class LogMessageReceivedEventArgs : EventArgs
    {
        public LogMessageType Type;
        public object Message;
        public Exception Exception;

        public LogMessageReceivedEventArgs(LogMessageType type, object message, Exception exception = null)
        {
            Type = type;
            Message = message;
            Exception = exception;
        }
    }
}
