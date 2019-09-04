using System.Threading.Tasks;

namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// Represents a feed that can be used to retrieve content items from Kentico Kontent Delivery API in smaller batches.
    /// </summary>
    public class DeliveryItemsFeed
    {
        internal delegate Task<DeliveryItemsFeedResponse> GetFeedResponse(string continuationToken);

        private DeliveryItemsFeedResponse _lastResponse;
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
        /// Initializes a new instance of <see cref="DeliveryItemsFeed"/> class.
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
        /// <returns>Instance of <see cref="DeliveryItemsFeedResponse"/> class that contains a list of content items.</returns>
        public async Task<DeliveryItemsFeedResponse> FetchNextBatchAsync()
        {
            if (HasMoreResults)
            {
                _lastResponse = await _getFeedResponseAsync(_lastResponse?.ContinuationToken);
            }

            return _lastResponse;
        }
    }
}