using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery.Tests
{
    [TestFixture]
    public class ContentItemTests
    {
        [TestCase]
        public void CastTo_GetStronglyTypedModularContent()
        {
            const string SANDBOX_PROJECT_ID = "e1167a11-75af-4a08-ad84-0582b463b010";
            var client = new DeliveryClient(SANDBOX_PROJECT_ID);

            ArticleModel parentArticle = client.GetItemAsync<ArticleModel>("article_1").Result.Item;
            //ArticleModel relatedArticle = parentArticle.RelatedArticles.First().CastTo<ArticleModel>();

            //Assert.AreEqual("Title of article 2", relatedArticle.Title);
            //Assert.AreEqual("Article 2", relatedArticle.System.Name);

            Assert.AreEqual("Title of article 2", parentArticle.RelatedArticles.First().GetString("title"));
        }
    }
}
