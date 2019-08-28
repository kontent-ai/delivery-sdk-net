using Newtonsoft.Json.Linq;
using System;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a successful JSON response from Kentico Cloud Delivery Delivery API. 
    /// </summary>
    public sealed class ApiResponse
    {
        /// <summary>
        /// Gets JSON content.
        /// </summary>
        public JObject Content { get; }

        /// <summary>
        /// Gets a value that determines if content is stale.
        /// Stale content indicates that there is a more recent version, but it will become available later.
        /// Stale content should be cached only for a limited period of time.
        /// </summary>
        public bool HasStaleContent { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiResponse"/> class.
        /// </summary>
        /// <param name="content">JSON content.</param>
        /// <param name="hasStaleContent">Specifies whether content is stale.</param>
        public ApiResponse(JObject content, bool hasStaleContent)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content), "Content is not specified.");
            HasStaleContent = hasStaleContent;
        }
    }
}
