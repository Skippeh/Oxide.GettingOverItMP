using System;

namespace ServerShared
{
    public class UnexpectedMessageFromClientException : Exception
    {
        public readonly MessageType MessageType;

        public UnexpectedMessageFromClientException(MessageType messageType)
        {
            MessageType = messageType;
        }
    }
}
