using System.Threading.Tasks;

namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// Represents a feed that can be used to retrieve strongly typed content items from Kentico Kontent Delivery API in smaller batches.
    /// </summary>
    /// <typeparam name="T">The type of content items in the feed.</typeparam>
    public class DeliveryItemsFeed<T>
    {
        internal delegate Task<DeliveryItemsFeedResponse<T>> GetFeedResponse(string continuationToken);

        private DeliveryItemsFeedResponse<T> _lastResponse;
        private readonly GetFeedResponse _getFeedResponseAsync;

        /// <summary>
        /// Indicates whether there are more batches to fetch.
        /// </summary>
        public bool HasMoreResults => _lastResponse == null || !string.IsNullOrEmpty(_lastResponse.ContinuationToken);

        /// <summary>
        /// Gets the URL used to retrieve responses in this feed for debugging purposes.
        /// </summary>
        public string ApiUrl { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="DeliveryItemsFeed{T}"/> class.
        /// </summary>
        /// <param name="getFeedResponseAsync">Function to retrieve next batch of content items.</param>
        /// <param name="requestUrl">URL used to retrieve responses for this feed.</param>
        internal DeliveryItemsFeed(GetFeedResponse getFeedResponseAsync, string requestUrl)
        {
            _getFeedResponseAsync = getFeedResponseAsync;
            ApiUrl = requestUrl;
        }

        /// <summary>
        /// Retrieves the next feed batch if available.
        /// </summary>
        /// <returns>Instance of <see cref="DeliveryItemsFeedResponse{T}"/> class that contains a list of strongly typed content items.</returns>
        public async Task<DeliveryItemsFeedResponse<T>> FetchNextBatchAsync()
        {
            if (HasMoreResults)
            {
                _lastResponse = await _getFeedResponseAsync(_lastResponse?.ContinuationToken);
            }

            return _lastResponse;
        }
    }
}