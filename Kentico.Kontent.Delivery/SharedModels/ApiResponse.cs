using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Kentico.Kontent.Delivery.SharedModels
{
    /// <summary>
    /// Represents a successful JSON response from Kentico Kontent Delivery API.
    /// </summary>
    [DebuggerDisplay("Url = {" + nameof(RequestUrl) + "}")]
    public sealed class ApiResponse : IApiResponse
    {
        private JObject _jsonContent;

        /// <inheritdoc/>
        public string Content { get; }

        /// <summary>
        /// Gets an object model of the JSON content.
        /// </summary>
        public JObject JsonContent => _jsonContent ??= Content != null ? JObject.Parse(Content) : null;

        /// <inheritdoc/>
        public bool HasStaleContent { get; }

        /// <inheritdoc/>
        public string ContinuationToken { get; }

        /// <inheritdoc/>
        public string RequestUrl { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiResponse"/> class.
        /// </summary>
        /// <param name="content">JSON content.</param>
        /// <param name="hasStaleContent">Specifies whether content is stale.</param>
        /// <param name="continuationToken">Continuation token to be used for continuing enumeration.</param>
        /// <param name="requestUrl">URL used to retrieve this response.</param>
        [JsonConstructor]
        internal ApiResponse(string content, bool hasStaleContent, string continuationToken, string requestUrl)
        {
            Content = content;
            HasStaleContent = hasStaleContent;
            ContinuationToken = continuationToken;
            RequestUrl = requestUrl;
        }
    }
}
