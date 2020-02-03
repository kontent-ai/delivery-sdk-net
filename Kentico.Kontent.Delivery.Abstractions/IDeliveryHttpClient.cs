using System.Net.Http;
using System.Threading.Tasks;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents a requests operations against Kentico Kontent Delivery API.
    /// </summary>
    public interface IDeliveryHttpClient
    {
        /// <summary>
        /// Returns a response message from Kentico Kontent Delivery API.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> SendHttpMessageAsync(HttpRequestMessage message);
    }
}
