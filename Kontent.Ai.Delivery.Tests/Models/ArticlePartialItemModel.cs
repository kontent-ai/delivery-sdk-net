using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Tests.Models
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