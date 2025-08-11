using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.ContentItems.Elements
{
    internal class ContentElementValue<T> : IContentElementValue<T>
    {
        /// <inheritdoc/>
        [JsonProperty("type")]
        public required string Type { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("value")]
        public required T Value { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("name")]
        public required string Name { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("codename")]
        public required string Codename { get; internal set; }
    }
}
