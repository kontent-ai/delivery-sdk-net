using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.RetryPolicy
{
    /// <summary>
    /// Provides a default retry policy implementation.
    /// Note: This is a minimal implementation for backward compatibility.
    /// Actual retry logic is handled by Microsoft.Extensions.Http.Resilience in the HTTP pipeline.
    /// </summary>
    internal class DefaultRetryPolicyProvider : IRetryPolicyProvider
    {
        /// <summary>
        /// Gets a retry policy instance.
        /// </summary>
        /// <returns>A minimal retry policy implementation.</returns>
        public IRetryPolicy GetRetryPolicy() => new NoOpRetryPolicy();
    }

    /// <summary>
    /// A no-op retry policy that doesn't actually retry.
    /// The real retry logic is handled by the HTTP pipeline.
    /// </summary>
    internal class NoOpRetryPolicy : IRetryPolicy
    {
        public async Task<HttpResponseMessage> ExecuteAsync(Func<Task<HttpResponseMessage>> sendAsync)
        {
            // Simply execute the request without retry
            // Actual retry is handled by resilience policies in the HTTP pipeline
            return await sendAsync();
        }
    }
}