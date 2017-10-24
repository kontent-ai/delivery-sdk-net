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
        /// <param name="responseStr">The error response.</param>
        public DeliveryException(HttpStatusCode statusCode, string responseStr)
        {
            StatusCode = statusCode;

            try
            {
                Message = JObject.Parse(responseStr)["message"].ToString();
            }
            catch (Exception)
            {
                Message = $"Unknown error. HTTP status code: {statusCode}.";
            }
        }
    }
}
