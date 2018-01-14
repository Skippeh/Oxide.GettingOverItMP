using System;

namespace GettingOverItMP.Updater.Exceptions
{
    public class ApiRequestFailedException : Exception
    {
        public ApiRequestFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
