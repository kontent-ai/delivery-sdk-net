using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.ContentItems.Elements
{
    internal class ContentElementValue<T> : IContentElementValue<T>
    {
        /// <inheritdoc/>
        [JsonProperty("type")]
        public string Type { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("value")]
        public T Value { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("name")]
        public string Name { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("codename")]
        public string Codename { get; internal set; }

        // TODO is that a good idea?
        public ContentElementValue<T> WithCodename(string codename)
        {
            Codename = codename;
            return this;
        }

    }
}
