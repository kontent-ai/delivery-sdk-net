using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

using Polly;

namespace KenticoCloud.Delivery.Helpers
{
    /// <summary>
    /// Delivery client helper.
    /// </summary>
    public static class DeliveryClientHelper
    {
        /// <summary>
        /// Key of the default resilience policy.
        /// </summary>
        public const string DEFAULT_POLICY_KEY = "KenticoCloud.Delivery.DefaultPolicyKey";

        /// <summary>
        /// Gets status codes that will induce retries.
        /// </summary>
        public static HttpStatusCode[] HttpStatusCodesWorthRetrying
        {
            get => new[]
                {
                   HttpStatusCode.RequestTimeout, // 408
                   HttpStatusCode.InternalServerError, // 500
                   HttpStatusCode.BadGateway, // 502
                   HttpStatusCode.ServiceUnavailable, // 503
                   HttpStatusCode.GatewayTimeout // 504
                };
        }

        /// <summary>
        /// Gets the default (fallback) resilience policy for HTTP requests.
        /// </summary>
        /// <param name="maxRetryAttempts">Maximum retry attempts.</param>
        /// <returns></returns>
        public static IAsyncPolicy<HttpResponseMessage> GetDefaultPolicy(int maxRetryAttempts)
        {
            // Only HTTP status codes are handled with retries, not exceptions.
            return Policy
                .HandleResult<HttpResponseMessage>(result => HttpStatusCodesWorthRetrying.Contains(result.StatusCode))
                .WaitAndRetryAsync(
                    maxRetryAttempts,
                    retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 100)
                    );
        }
    }
}
