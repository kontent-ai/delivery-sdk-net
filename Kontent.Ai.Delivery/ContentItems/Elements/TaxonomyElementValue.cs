using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems.Elements
{
    internal class TaxonomyElementValue : ContentElementValue<IEnumerable<ITaxonomyTerm>>, ITaxonomyElementValue
    {
        [JsonPropertyName("taxonomy_group")]
        public required string TaxonomyGroup { get; set; }
    }
}
