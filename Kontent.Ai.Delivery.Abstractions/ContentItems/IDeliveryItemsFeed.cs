using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Represents a feed that can be used to retrieve strongly typed content items from Kontent.ai Delivery API in smaller batches.
    /// </summary>
    /// <typeparam name="TModel">The type of content items in the feed.</typeparam>
    public interface IDeliveryItemsFeed<TModel>
        where TModel : IElementsModel
    {
        /// <summary>
        /// Indicates whether there are more batches to fetch.
        /// </summary>
        bool HasMoreResults { get; }

        /// <summary>
        /// Retrieves the next feed batch if available.
        /// </summary>
        /// <param name="continuationToken">Optional explicit continuation token that allows you to get the next batch from a specific point in the feed.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Instance of <see cref="IDeliveryItemsFeedResponse{T}"/> class that contains a list of strongly typed content items.</returns>
        Task<IDeliveryItemsFeedResponse<TModel>> FetchNextBatchAsync(string? continuationToken = null, CancellationToken ct = default);
    }
}
