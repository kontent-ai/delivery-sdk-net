using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// Represents a successful JSON response from Kentico Kontent Delivery API.
    /// </summary>
    public sealed class ApiResponse
    {
        /// <summary>
        /// Gets JSON content.
        /// </summary>
        public JObject Content { get; }

        /// <summary>
        /// Gets a value that determines whether content is stale.
        /// Stale content indicates that there is a more recent version, but it will become available later.
        /// Stale content should be cached only for a limited period of time.
        /// </summary>
        public bool HasStaleContent { get; }

        /// <summary>
        /// Gets the continuation token to be used for continuing enumeration of the Kentico Kontent Delivery API.
        /// </summary>
        public string ContinuationToken { get; }

        /// <summary>
        /// Gets the URL used to retrieve this response for debugging purposes.
        /// </summary>
        public string RequestUrl { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiResponse"/> class.
        /// </summary>
        /// <param name="content">JSON content.</param>
        /// <param name="hasStaleContent">Specifies whether content is stale.</param>
        /// <param name="continuationToken">Continuation token to be used for continuing enumeration.</param>
        /// <param name="requestUrl">URL used to retrieve this response.</param>
        internal ApiResponse(JObject content, bool hasStaleContent, string continuationToken, string requestUrl)
        {
            Content = content;
            HasStaleContent = hasStaleContent;
            ContinuationToken = continuationToken;
            RequestUrl = requestUrl;
        }
    }
}
