using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Models.Item;
using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Tests
{
    public class ArticlePartialItemModel
    {
        public const string Codename = "article";
        public string Title { get; set; }
        public string Summary { get; set; }
        public IEnumerable<TaxonomyTerm> Personas { get; set; }
        public ContentItemSystemAttributes System { get; set; }
    }
}