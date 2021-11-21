using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents a retry policy for HTTP requests.
    /// </summary>
    public interface IRetryPolicy
    {
        /// <summary>
        /// Invokes the specified request delegate within policy.
        /// </summary>
        /// <param name="sendRequest">The request delegate to invoke.</param>
        /// <returns>A response message returned by <paramref name="sendRequest"/>.</returns>
        Task<HttpResponseMessage> ExecuteAsync(Func<Task<HttpResponseMessage>> sendRequest);
    }
}