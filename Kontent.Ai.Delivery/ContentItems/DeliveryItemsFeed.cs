using System;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.ContentItems
{
    /// <summary>
    /// Represents a feed that can be used to retrieve strongly typed content items from Kontent.ai Delivery API in smaller batches.
    /// </summary>
    /// <typeparam name="T">The type of content items in the feed.</typeparam>
    internal class DeliveryItemsFeed<T> : IDeliveryItemsFeed<T>
    {
        internal delegate Task<DeliveryItemsFeedResponse<T>> GetFeedResponse(string continuationToken);

        private string _continuationToken;
        private readonly GetFeedResponse _getFeedResponseAsync;

        /// <summary>
        /// Indicates whether there are more batches to fetch.
        /// </summary>
        public bool HasMoreResults { get; private set; } = true;

        /// <summary>
        /// Initializes a new instance of <see cref="DeliveryItemsFeed{T}"/> class.
        /// </summary>
        /// <param name="getFeedResponseAsync">Function to retrieve next batch of content items.</param>
        public DeliveryItemsFeed(GetFeedResponse getFeedResponseAsync)
        {
            _getFeedResponseAsync = getFeedResponseAsync;
        }

        /// <summary>
        /// Retrieves the next feed batch if available.
        /// </summary>
        /// <param name="continuationToken">Optional explicit continuation token that allows you to get the next batch from a specific point in the feed.</param>
        /// <returns>Instance of <see cref="DeliveryItemsFeedResponse{T}"/> class that contains a list of strongly typed content items.</returns>
        public async Task<IDeliveryItemsFeedResponse<T>> FetchNextBatchAsync(string continuationToken = null)
        {
            if (!HasMoreResults)
            {
                throw new InvalidOperationException("The feed has already been enumerated and there are no more results.");
            }

            var response = await _getFeedResponseAsync(continuationToken ?? _continuationToken);

            if (!response.ApiResponse.IsSuccess)
            {
                return new DeliveryItemsFeedResponse<T>(response.ApiResponse, null);
            }
            
            _continuationToken = response.ApiResponse.ContinuationToken;
            HasMoreResults = !string.IsNullOrEmpty(response.ApiResponse.ContinuationToken);

            return response;
        }
    }
}