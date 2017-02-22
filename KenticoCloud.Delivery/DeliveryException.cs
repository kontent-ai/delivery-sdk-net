using Newtonsoft.Json.Linq;
using System;
using System.Net;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents an error response from the Kentico Cloud Delivery API.
    /// </summary>
    public sealed class DeliveryException : Exception
    {
        /// <summary>
        /// Gets the HTTP status code of the response.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Gets the error message from the response.
        /// </summary>
        public override string Message { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryException"/> class with information from an error response.
        /// </summary>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="message">The error message from the response.</param>
        public DeliveryException(HttpStatusCode statusCode, string message)
        {
            StatusCode = statusCode;
            Message = JObject.Parse(message)["message"].ToString();
        }
    }
}
