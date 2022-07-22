using System.Net.Http;
using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Represents a requests operations against Kontent.ai Delivery API.
    /// </summary>
    public interface IDeliveryHttpClient
    {
        /// <summary>
        /// Returns a response message from Kontent.ai Delivery API.
        /// </summary>
        /// <param name="message">HttpRequestMessage instance represents the request message</param>
        /// <returns>Returns a HttpResponseMessage from Kontent.ai Delivery API</returns>
        Task<HttpResponseMessage> SendHttpMessageAsync(HttpRequestMessage message);
    }
}
