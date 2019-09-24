using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Options;
using Polly;

namespace Kentico.Kontent.Delivery.ResiliencePolicy
{
    /// <summary>
    /// Provides a default (fallback) retry policy for HTTP requests
    /// </summary>
    internal class DefaultResiliencePolicyProvider : IResiliencePolicyProvider
    {
        private static readonly HttpStatusCode[] HttpStatusCodesWorthRetrying = 
        {
            HttpStatusCode.RequestTimeout, // 408
            HttpStatusCode.InternalServerError, // 500
            HttpStatusCode.BadGateway, // 502
            HttpStatusCode.ServiceUnavailable, // 503
            HttpStatusCode.GatewayTimeout // 504
        };

        private readonly IOptions<DeliveryOptions> _deliveryOptions;

        private int MaxRetryOptions => _deliveryOptions.Value.MaxRetryAttempts;

        /// <summary>
        /// Creates a default retry policy provider with a maximum number of retry attempts.
        /// </summary>
        /// <param name="deliveryOptions">Options containing maximum retry attempts for a request.</param>
        public DefaultResiliencePolicyProvider(IOptions<DeliveryOptions> deliveryOptions)
        {
            _deliveryOptions = deliveryOptions;
        }

        /// <summary>
        /// Gets the default (fallback) retry policy for HTTP requests.
        /// </summary>
        public IAsyncPolicy<HttpResponseMessage> Policy
        {
            get
            {
                // Only HTTP status codes are handled with retries, not exceptions.
                return Polly.Policy
                    .HandleResult<HttpResponseMessage>(result => HttpStatusCodesWorthRetrying.Contains(result.StatusCode))
                    .WaitAndRetryAsync(
                        MaxRetryOptions,
                        retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 100)
                        );
            }
        }
    }
}
