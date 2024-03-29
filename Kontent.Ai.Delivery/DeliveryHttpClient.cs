﻿using System.Net.Http;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Extensions;

namespace Kontent.Ai.Delivery
{
    /// <summary>
    /// Executes Http requests against the Kontent.ai Delivery API.
    /// </summary>
    public class DeliveryHttpClient : IDeliveryHttpClient
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="IDeliveryHttpClient"/> class.
        /// </summary>
        /// <param name="httpClient">An HTTP client instance (this can be provided manually or via DI - for example by the HttpClientFactory)</param>
        public DeliveryHttpClient(HttpClient httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();

            _httpClient.DefaultRequestHeaders.AddSdkTrackingHeader();
            _httpClient.DefaultRequestHeaders.AddSourceTrackingHeader();
        }

        /// <summary>
        /// Returns a response message from Kontent.ai Delivery API.
        /// </summary>
        /// <param name="message">HttpRequestMessage instance represents the request message</param>
        /// <returns>Returns a HttpResponseMessage from Kontent.ai Delivery API</returns>
        public async Task<HttpResponseMessage> SendHttpMessageAsync(HttpRequestMessage message)
        {
            return await _httpClient.SendAsync(message);
        }
    }
}
