using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions;

namespace Kentico.Kontent.Delivery.Tests.Models
{
    public class ArticlePartialItemModel
    {
        public const string Codename = "article";
        public string Title { get; set; }
        public string Summary { get; set; }
        public IEnumerable<ITaxonomyTerm> Personas { get; set; }
        public IContentItemSystemAttributes System { get; set; }
    }
}