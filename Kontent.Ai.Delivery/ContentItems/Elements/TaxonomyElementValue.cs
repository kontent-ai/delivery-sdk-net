using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Kontent.Ai.Delivery.ContentItems.Elements
{
    internal class TaxonomyElementValue : ContentElementValue<IEnumerable<ITaxonomyTerm>>, ITaxonomyElementValue
    {
        [JsonProperty("taxonomy_group")]
        public string TaxonomyGroup { get; set; }
    }
}
