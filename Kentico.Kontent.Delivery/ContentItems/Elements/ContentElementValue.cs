using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.ContentItems.Elements
{
    internal sealed class ContentElementValue<T> : IContentElementValue<T>
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

        public IReadOnlyList<IMultipleChoiceOption> Options => throw new System.NotImplementedException();

        public string TaxonomyGroup => throw new System.NotImplementedException();

        public ContentElementValue()
        {
        }
    }
}
