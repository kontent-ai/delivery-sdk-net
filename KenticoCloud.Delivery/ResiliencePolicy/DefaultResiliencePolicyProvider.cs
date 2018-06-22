using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

using Polly;

namespace KenticoCloud.Delivery.ResiliencePolicy
{
    /// <summary>
    /// Provides a default (fallback) retry policy for HTTP requests
    /// </summary>
    public class DefaultResiliencePolicyProvider : IResiliencePolicyProvider
    {
        private int _maxRetryAttempts;

        /// <summary>
        /// Creates a default retry policy provider with a maximum number of retry attempts.
        /// </summary>
        /// <param name="maxRetryAttempts">Maximum retry attempts for a request.</param>
        public DefaultResiliencePolicyProvider(int maxRetryAttempts)
        {
            _maxRetryAttempts = maxRetryAttempts;
        }

        private HttpStatusCode[] _httpStatusCodesWorthRetrying => new[]
            {
                HttpStatusCode.RequestTimeout, // 408
                HttpStatusCode.InternalServerError, // 500
                HttpStatusCode.BadGateway, // 502
                HttpStatusCode.ServiceUnavailable, // 503
                HttpStatusCode.GatewayTimeout // 504
            };

        /// <summary>
        /// Gets the default (fallback) retry policy for HTTP requests.
        /// </summary>
        public IAsyncPolicy<HttpResponseMessage> Policy
        {
            get
            {
                // Only HTTP status codes are handled with retries, not exceptions.
                return Polly.Policy
                    .HandleResult<HttpResponseMessage>(result => _httpStatusCodesWorthRetrying.Contains(result.StatusCode))
                    .WaitAndRetryAsync(
                        _maxRetryAttempts,
                        retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 100)
                        );
            }
        }
    }
}
