using System;
using System.Net;
using Newtonsoft.Json.Linq;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents an error response from the API.
    /// </summary>
    public class DeliverException : Exception
    {
        /// <summary>
        /// HTTP status code.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Detailed message from the API.
        /// </summary>
        public override string Message { get; }

        /// <summary>
        /// Initializes exception.
        /// </summary>
        /// <param name="statusCode">Status code of response.</param>
        /// <param name="message">Exception message.</param>
        public DeliverException(HttpStatusCode statusCode, string message)
        {
            var errorMessage = JObject.Parse(message);

            StatusCode = statusCode;
            Message = errorMessage["message"].ToString();
        }
    }
}
