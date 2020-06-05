namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents a successful JSON response from Kentico Kontent Delivery API.
    /// </summary>
    public interface IApiResponse
    {
        /// <summary>
        /// Gets JSON content.
        /// </summary>
        string Content { get; }

        /// <summary>
        /// Gets the continuation token to be used for continuing enumeration of the Kentico Kontent Delivery API.
        /// </summary>
        string ContinuationToken { get; }

        /// <summary>
        /// Gets a value that determines whether content is stale.
        /// Stale content indicates that there is a more recent version, but it will become available later.
        /// Stale content should be cached only for a limited period of time.
        /// </summary>
        bool HasStaleContent { get; }

        /// <summary>
        /// Gets the URL used to retrieve this response for debugging purposes.
        /// </summary>
        string RequestUrl { get; }
    }
}