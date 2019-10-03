using System;
using System.Threading.Tasks;

namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// Represents a feed that can be used to retrieve content items from Kentico Kontent Delivery API in smaller batches.
    /// </summary>
    public class DeliveryItemsFeed
    {
        internal delegate Task<DeliveryItemsFeedResponse> GetFeedResponse(string continuationToken);

        private string _continuationToken;
        private readonly GetFeedResponse _getFeedResponseAsync;

        /// <summary>
        /// Indicates whether there are more batches to fetch.
        /// </summary>
        public bool HasMoreResults { get; private set; } = true;

        /// <summary>
        /// Initializes a new instance of <see cref="DeliveryItemsFeed"/> class.
        /// </summary>
        /// <param name="getFeedResponseAsync">Function to retrieve next batch of content items.</param>
        internal DeliveryItemsFeed(GetFeedResponse getFeedResponseAsync)
        {
            _getFeedResponseAsync = getFeedResponseAsync;
        }

        /// <summary>
        /// Retrieves the next feed batch if available.
        /// </summary>
        /// <returns>Instance of <see cref="DeliveryItemsFeedResponse"/> class that contains a list of content items.</returns>
        public async Task<DeliveryItemsFeedResponse> FetchNextBatchAsync()
        {
            if (!HasMoreResults)
            {
                throw new InvalidOperationException("The feed has already been enumerated and there are no more results.");
            }

            var response = await _getFeedResponseAsync(_continuationToken);
            _continuationToken = response.ContinuationToken;
            HasMoreResults = !string.IsNullOrEmpty(response.ContinuationToken);

            return response;
        }
    }
}