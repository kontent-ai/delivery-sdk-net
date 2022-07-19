using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.SharedModels
{
    internal sealed class Error : IError
    {
        /// <inheritdoc/>
        [JsonProperty("message")]
        public string Message { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("request_id")]
        public string RequestId { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("error_code")]
        public int ErrorCode { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("specific_code")]
        public int SpecificCode { get; internal set; }
    }
}
