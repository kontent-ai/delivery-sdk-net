using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Represents a successful JSON response from Kontent.ai Delivery API.
    /// </summary>
    public interface IApiResponse
    {
        /// <summary>
        /// Gets JSON content.
        /// </summary>
        string Content { get; }

        /// <summary>
        /// Gets the continuation token to be used for continuing enumeration of the Kontent.ai Delivery API.
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

        /// <summary>
        /// Indicates whether a call to Kontent.ai Delivery API was successful or not.
        /// </summary>
        bool IsSuccess { get; }

        /// <summary>
        /// Gets error object with message, error code.
        /// </summary>
        IError Error { get; }

        public Task<object> GetJsonContentAsync();
    }
}
