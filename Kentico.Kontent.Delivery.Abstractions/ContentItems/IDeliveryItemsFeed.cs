using System.Threading.Tasks;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents a feed that can be used to retrieve strongly typed content items from Kontent Delivery API in smaller batches.
    /// </summary>
    /// <typeparam name="T">The type of content items in the feed.</typeparam>
    public interface IDeliveryItemsFeed<T>
    {
        /// <summary>
        /// Indicates whether there are more batches to fetch.
        /// </summary>
        bool HasMoreResults { get; }

        /// <summary>
        /// Retrieves the next feed batch if available.
        /// </summary>
        /// <returns>Instance of <see cref="IDeliveryItemsFeedResponse{T}"/> class that contains a list of strongly typed content items.</returns>
        Task<IDeliveryItemsFeedResponse<T>> FetchNextBatchAsync();
    }
}
