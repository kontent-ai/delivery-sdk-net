using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentTypes.Element
{
    internal class TaxonomyElement : ContentElement, ITaxonomyElement
    {
        /// <inheritdoc/>
        [JsonPropertyName("taxonomy_group")]
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
