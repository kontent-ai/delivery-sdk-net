using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Kentico.Kontent.Delivery.RetryPolicy
{
    /// <summary>
    /// Requests handling retry policy
    /// </summary>
    public interface IRetryPolicy
    {
        /// <summary>
        /// Executes the specified asynchronous request within policy and returns result
        /// </summary>
        /// <param name="sendRequest">The action to perform</param>
        /// <returns>A response message returned by <paramref name="sendRequest"/>.</returns>
        Task<HttpResponseMessage> ExecuteAsync(Func<Task<HttpResponseMessage>> sendRequest);
    }
}