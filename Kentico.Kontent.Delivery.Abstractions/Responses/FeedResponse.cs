namespace Kentico.Kontent.Delivery.Abstractions.Responses
{
    /// <summary>
    /// Represents a successful response from Kentico Kontent Delivery API enumeration.
    /// </summary>
    public abstract class FeedResponse : AbstractResponse
    {
        /// <summary>
        /// Gets the continuation token to be used for continuing enumeration of the Kentico Kontent Delivery API.
        /// </summary>
        public string ContinuationToken => ApiResponse.ContinuationToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeedResponse"/> class.
        /// </summary>
        /// <param name="apiResponse">A successful JSON response from Kentico Kontent Delivery API.</param>
        protected FeedResponse(IApiResponse apiResponse) : base(apiResponse)
        {
        }
    }
}