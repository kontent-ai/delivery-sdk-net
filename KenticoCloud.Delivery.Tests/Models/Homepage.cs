using System.Collections.Generic;

namespace KenticoKontent.Delivery.Tests
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