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
        // TODO: Unable to build due to change in visiblity level of ContentItem class from public to internal.
        //[TestCase]
        //public void CastTo_GetStronglyTypedModularContent()
        //{
//            var jsonDefinition = @"{
//   ""item"":{
//      ""system"":{
//         ""id"":""cd17ddf8-7bc6-47de-8beb-0e15a2381d76"",
//         ""name"":""Parent"",
//         ""codename"":""parent"",
//         ""type"":""article"",
//         ""sitemap_locations"":[],
//         ""last_modified"":""2017-02-22T00:52:03.9370993Z""
//      },
//      ""elements"":{
//         ""title"":{
//            ""type"":""text"",
//            ""name"":""Title"",
//            ""value"":""Title of Article""
//         },
//         ""related_articles"":{
//            ""type"":""modular_content"",
//            ""name"":""Modular content field"",
//            ""value"":[
//               ""article2""
//            ]
//         }
//      }
//   },
//   ""modular_content"":{
//      ""article2"":{
//         ""system"":{
//            ""id"":""b30d533f-9822-4518-8113-e4b3437641b5"",
//            ""name"":""Article 2"",
//            ""codename"":""article2"",
//            ""type"":""article"",
//            ""sitemap_locations"":[],
//            ""last_modified"":""2017-02-21T04:29:42.9564205Z""
//         },
//         ""elements"":{
//            ""title"":{
//               ""type"":""text"",
//               ""name"":""Title"",
//               ""value"":""Title of Article 2""
//            },
//            ""related_articles"":{
//                ""type"":""modular_content"",
//                ""name"":""Modular content field"",
//                ""value"":[]
//            }
//         }
//      }
//   }
//}";
//            JObject contentItem = JObject.Parse(jsonDefinition);
//            ArticleModel parentArticle = new ContentItem(contentItem["item"], contentItem["modular_content"]).CastTo<ArticleModel>();
//            ArticleModel relatedArticle = parentArticle.RelatedArticles.First().CastTo<ArticleModel>();

//            Assert.AreEqual("Title of Article 2", relatedArticle.Title);
//            Assert.AreEqual("Article 2", relatedArticle.System.Name);
        //}
    }
}
