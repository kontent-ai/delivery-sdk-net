using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.ContentItems.Elements
{
    internal class TaxonomyElementValue : ContentElementValue<IEnumerable<ITaxonomyTerm>>, ITaxonomyElementValue
    {
        [JsonProperty("taxonomy_group")]
        public string TaxonomyGroup { get; set; }

        public TaxonomyElementValue() : base()
        {
        }
    }
}
