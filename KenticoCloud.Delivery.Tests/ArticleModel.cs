using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace KenticoCloud.Delivery.Tests
{
    public class ArticleModel
    {
        public string Title { get; set; }
        public IEnumerable<ContentItem> RelatedArticles { get; set; }
    }
}
