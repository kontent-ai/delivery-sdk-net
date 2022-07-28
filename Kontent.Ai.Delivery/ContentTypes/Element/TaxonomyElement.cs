using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.ContentTypes.Element
{
    internal class TaxonomyElement : ContentElement, ITaxonomyElement
    {
        /// <inheritdoc/>
        [JsonProperty("taxonomy_group")]
        public string TaxonomyGroup { get; internal set; }

        /// <summary>
        /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
        /// </summary>
        [JsonConstructor]
        public TaxonomyElement()
        {
        }
    }
}
