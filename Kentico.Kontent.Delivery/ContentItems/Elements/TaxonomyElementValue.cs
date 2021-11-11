using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;
using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions.ContentItems.Elements;
using Kentico.Kontent.Delivery.Abstractions.SharedModels;

namespace Kentico.Kontent.Delivery.ContentItems.Elements
{
    internal class TaxonomyElementValue : ContentElementValue<IEnumerable<ITaxonomyTerm>>, ITaxonomyElementValue
    {
        [JsonProperty("taxonomy_group")]
        public string TaxonomyGroup { get; set; }
    }
}
