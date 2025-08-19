using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.ContentItems.Elements
{
    internal class TaxonomyElementValue : ContentElementValue<IEnumerable<ITaxonomyTerm>>, ITaxonomyElementValue
    {
        [JsonProperty("taxonomy_group")]
        public required string TaxonomyGroup { get; set; }
    }
}
