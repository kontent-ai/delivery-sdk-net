using System.Collections.Generic;
using Kentico.Kontent.Delivery.ContentItems;
using Kentico.Kontent.Delivery.TaxonomyGroups;

namespace Kentico.Kontent.Delivery.Tests.Models
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