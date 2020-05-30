using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Models.Item;
using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Tests
{
    public partial class Homepage
    {
        public string CallToAction { get; set; }
        public string Subtitle { get; set; }
        public IEnumerable<Asset> Image { get; set; }
        public IEnumerable<TaxonomyTerm> UntitledTaxonomyGroup { get; set; }
        public ContentItemSystemAttributes System { get; set; }
    }
}