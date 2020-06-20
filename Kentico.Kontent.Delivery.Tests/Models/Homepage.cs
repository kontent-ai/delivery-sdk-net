using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.ContentItems;

namespace Kentico.Kontent.Delivery.Tests.Models
{
    public class Homepage
    {
        public string CallToAction { get; set; }
        public string Subtitle { get; set; }
        public IEnumerable<IAsset> Image { get; set; }
        public IEnumerable<ITaxonomyTerm> UntitledTaxonomyGroup { get; set; }
        public IContentItemSystemAttributes System { get; set; }
    }
}