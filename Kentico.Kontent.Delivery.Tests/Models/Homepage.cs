using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.Tests.Models
{
    public class Homepage
    {
        public string CallToAction { get; set; }
        public string Subtitle { get; set; }
        public IEnumerable<IAsset> Image { get; set; }
        public IEnumerable<ITaxonomyTerm> UntitledTaxonomyGroup { get; set; }
        public IContentItemSystemAttributes System { get; set; }
        public IEnumerable<Page> Subpages { get; set; }
       
        [JsonProperty("partnership_events")]
        public IEnumerable<PartnershipPage> Partnerships { get; set; }
    }
}