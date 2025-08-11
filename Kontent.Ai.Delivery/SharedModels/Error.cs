using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.SharedModels
{
    internal sealed class Error : IError
    {
        /// <inheritdoc/>
        [JsonProperty("message")]
        public required string Message { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("request_id")]
        public required string RequestId { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("error_code")]
        public int ErrorCode { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("specific_code")]
        public int SpecificCode { get; internal set; }
    }
}
