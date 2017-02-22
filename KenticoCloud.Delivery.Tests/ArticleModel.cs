using System;
using System.Collections.Generic;

namespace KenticoCloud.Delivery.Tests
{
    public class ArticleModel : IContentItemBased
    {
        public string Title { get; set; }
        public IEnumerable<ContentItem> RelatedArticles { get; set; }
        public ItemSystem System { get; set; }

        public void LoadFromContentItem(ContentItem contentItem)
        {
            Title = contentItem.GetString("title");
            RelatedArticles = contentItem.GetModularContent("related_articles");
            System = contentItem.System;
        }
    }
}
