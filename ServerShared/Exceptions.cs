using System;

namespace ServerShared
{
    public class UnexpectedMessageFromClientException : Exception
    {
        public UnexpectedMessageFromClientException(MessageType messageType)
        {
        }
    }
}
