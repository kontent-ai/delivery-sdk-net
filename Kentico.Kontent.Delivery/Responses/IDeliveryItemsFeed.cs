using System.Threading.Tasks;

namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// Represents a feed that can be used to retrieve content items from Kentico Kontent Delivery API in smaller batches.
    /// </summary>
    public interface IDeliveryItemsFeed
    {
        /// <summary>
        /// Indicates whether there are more batches to fetch.
        /// </summary>
        bool HasMoreResults { get; }

        /// <summary>
        /// Retrieves the next feed batch if available.
        /// </summary>
        /// <returns>Instance of <see cref="DeliveryItemsFeedResponse"/> class that contains a list of content items.</returns>
        Task<DeliveryItemsFeedResponse> FetchNextBatchAsync();
    }
}
