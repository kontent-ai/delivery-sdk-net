using System.Collections.Generic;

namespace KenticoCloud.Delivery.Tests
{

    public partial class Article
    {
        public string Title { get; set; }
        public IEnumerable<object> RelatedArticles { get; set; }
    }
}
