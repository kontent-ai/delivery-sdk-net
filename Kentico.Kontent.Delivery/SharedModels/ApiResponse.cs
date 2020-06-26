using Kentico.Kontent.Delivery.Abstractions;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Kentico.Kontent.Delivery.SharedModels
{
    /// <summary>
    /// Represents a successful JSON response from Kentico Kontent Delivery API.
    /// </summary>
    [DebuggerDisplay("Url = {" + nameof(RequestUrl) + "}")]
    internal sealed class ApiResponse : IApiResponse
    {
        private JObject _jsonContent;
        private string _content;

        /// <inheritdoc/>
        [JsonIgnore]
        public HttpContent HttpContent { get; }

        /// <inheritdoc/>
        public string Content
        {
            get
            {
                return _content ??= Task.Run(() => HttpContent.ReadAsStringAsync()).GetAwaiter().GetResult();
            }
            set
            {
                _content = value;
            }
        }

        /// <inheritdoc/>
        public bool HasStaleContent { get; }

        /// <inheritdoc/>
        public string ContinuationToken { get; }

        /// <inheritdoc/>
        public string RequestUrl { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiResponse"/> class.
        /// </summary>
        /// <param name="httpContent">HTTP body content.</param>
        /// <param name="hasStaleContent">Specifies whether content is stale.</param>
        /// <param name="continuationToken">Continuation token to be used for continuing enumeration.</param>
        /// <param name="requestUrl">URL used to retrieve this response.</param>
        internal ApiResponse(HttpContent httpContent, bool hasStaleContent, string continuationToken, string requestUrl)
        {
            HttpContent = httpContent;
            HasStaleContent = hasStaleContent;
            ContinuationToken = continuationToken;
            RequestUrl = requestUrl;
        }

        /// <summary>
        /// An internal constructor used for deserialization (useful for caching scenarios, etc.)
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

        /// <summary>
        /// Gets an object model of the JSON content.
        /// </summary>
        public async Task<JObject> GetJsonContentAsync()
        {
            if (_jsonContent == null)
            {
                using var streamReader = new HttpRequestStreamReader(await HttpContent.ReadAsStreamAsync(), Encoding.UTF8);
                using var jsonReader = new JsonTextReader(streamReader);
                _jsonContent = await JObject.LoadAsync(jsonReader);
            }
            return _jsonContent;
        }
    }
}
