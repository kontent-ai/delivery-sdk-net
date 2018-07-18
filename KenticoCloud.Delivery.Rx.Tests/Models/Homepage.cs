using System;
using System.Collections.Generic;
using KenticoCloud.Delivery;
using Newtonsoft.Json;

namespace KenticoCloud.Delivery.Rx.Tests
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