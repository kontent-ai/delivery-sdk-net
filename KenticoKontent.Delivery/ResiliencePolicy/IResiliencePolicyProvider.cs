using System.Net.Http;
using Polly;

namespace KenticoKontent.Delivery.ResiliencePolicy
{
    /// <summary>
    /// Provides a resilience policy.
    /// </summary>
    public interface IResiliencePolicyProvider
    {
        /// <summary>
        /// Gets the resilience policy.
        /// </summary>
        IAsyncPolicy<HttpResponseMessage> Policy { get; }
    }
}