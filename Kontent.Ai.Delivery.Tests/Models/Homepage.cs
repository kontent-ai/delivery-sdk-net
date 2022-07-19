using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Tests.Models
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