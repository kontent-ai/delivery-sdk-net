using System;
using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a successful JSON response from Kentico Cloud Delivery API.
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
        /// Gets the URL used to retrieve this response for debugging purposes.
        /// </summary>
        public string RequestUrl { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiResponse"/> class.
        /// </summary>
        /// <param name="content">JSON content.</param>
        /// <param name="hasStaleContent">Specifies whether content is stale.</param>
        /// <param name="requestUrl">URL used to retrieve this response.</param>
        public ApiResponse(JObject content, bool hasStaleContent, string requestUrl)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content), "Content is not specified.");
            }

            if (requestUrl is null)
            {
                throw new ArgumentNullException(nameof(requestUrl), "Request URL is not specified.");
            }

            if (requestUrl == string.Empty)
            {
                throw new ArgumentException("Request URL is not specified.", nameof(requestUrl));
            }

            Content = content;
            HasStaleContent = hasStaleContent;
            RequestUrl = requestUrl;
        }
    }
}
