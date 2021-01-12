using System;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// Represents an error response from the Kentico Kontent Delivery API.
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
        /// <param name="response">The unsuccessful response.</param>
        public DeliveryException(HttpResponseMessage response)
        {
            Message = $"Unknown error. HTTP status code: {response?.StatusCode}. Reason phrase: {response?.ReasonPhrase}.";
        }
    }
}