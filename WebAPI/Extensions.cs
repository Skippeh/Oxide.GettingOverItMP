using Nancy;

namespace WebAPI
{
    internal static class Extensions
    {
        public static Response Empty(this IResponseFormatter responseFormatter, HttpStatusCode statusCode)
        {
            var result = new Response();
            result.StatusCode = statusCode;
            return result;
        }

        public static Response JsonError(this IResponseFormatter responseFormatter, string message, HttpStatusCode statusCode)
        {
            return responseFormatter.AsJson(message == null ? null : new
            {
                error = message
            }, statusCode);
        }

        public static Response Error(this IResponseFormatter responseFormatter, string message, HttpStatusCode statusCode)
        {
            var result = new Response();
            result.StatusCode = statusCode;
            result.ReasonPhrase = message;
            return result;
        }
    }
}
