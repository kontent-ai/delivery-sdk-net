using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a generic response from Kentico Cloud Delivery API.
    /// </summary>
    public abstract class AbstractResponse
    {
        /// <summary>
        /// The successful JSON response from Kentico Cloud Delivery API.
        /// </summary>
        protected readonly ApiResponse _response;

        /// <summary>
        /// Gets a value that determines whether content is stale.
        /// Stale content indicates that there is a more recent version, but it will become available later.
        /// Stale content should be cached only for a limited period of time.
        /// </summary>
        public bool HasStaleContent => _response.HasStaleContent;

        /// <summary>
        /// Gets the URL used to retrieve this response for debugging purposes.
        /// </summary>
        public string ApiUrl => _response.RequestUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractResponse"/> class.
        /// </summary>
        /// <param name="response">A successful JSON response from Kentico Cloud Delivery API.</param>
        protected AbstractResponse(ApiResponse response)
        {
            _response = response;
        }
    }
}
