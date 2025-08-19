using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.SharedModels
{
    internal sealed class Error : IError
    {
        /// <inheritdoc/>
        [JsonPropertyName("message")]
        public required string Message { get; internal set; }

        /// <inheritdoc/>
        [JsonPropertyName("request_id")]
        public required string RequestId { get; internal set; }

        /// <inheritdoc/>
        [JsonPropertyName("error_code")]
        public int ErrorCode { get; internal set; }

        /// <inheritdoc/>
        [JsonPropertyName("specific_code")]
        public int SpecificCode { get; internal set; }
    }
}
