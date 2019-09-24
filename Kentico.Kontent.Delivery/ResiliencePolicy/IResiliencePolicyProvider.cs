using System.Net.Http;
using Polly;

namespace Kentico.Kontent.Delivery.ResiliencePolicy
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