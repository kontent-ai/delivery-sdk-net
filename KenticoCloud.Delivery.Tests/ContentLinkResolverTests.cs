using Newtonsoft.Json.Linq;
using RichardSzalay.MockHttp;
using System;
using System.IO;
using Xunit;

namespace KenticoCloud.Delivery.Tests
{
    public class ContentLinkResolverTests
    {
        [Fact]
        public void ContentLinkIsResolved()
        {
            var result = ResolveContentLinks("Learn <a href=\"\" data-item-id=\"CID\">more</a>.");

            Assert.Equal("Learn <a href=\"http://example.org/about-us\" data-item-id=\"CID\">more</a>.", result);
        }

        [Fact]
        public void DecoratedContentLinkIsResolved()
        {
            var result = ResolveContentLinks("Learn <a href=\"\" data-item-id=\"CID\" class=\"link\">more</a>.");

            Assert.Equal("Learn <a href=\"http://example.org/about-us\" data-item-id=\"CID\" class=\"link\">more</a>.", result);
        }

        [Fact]
        public void BrokenContentLinkIsResolved()
        {
            var result = ResolveContentLinks("Learn <a href=\"\" data-item-id=\"OTHER\">more</a>.");

            Assert.Equal("Learn <a href=\"http://example.org/broken\" data-item-id=\"OTHER\">more</a>.", result);
        }

        [Fact]
        public void ResolveLinkUrlIsOptional()
        {
            var linkUrlResolver = new CustomContentLinkUrlResolver
            {
                GetLinkUrl = link => null
            };
            var result = ResolveContentLinks("Learn <a href=\"\" data-item-id=\"CID\">more</a>.", linkUrlResolver);

            Assert.Equal("Learn <a href=\"\" data-item-id=\"CID\">more</a>.", result);
        }

        [Fact]
        public void ResolveBrokenLinkUrlIsOptional()
        {
            var linkUrlResolver = new CustomContentLinkUrlResolver
            {
                GetBrokenLinkUrl = () => null
            };
            var result = ResolveContentLinks("Learn <a href=\"\" data-item-id=\"OTHER\">more</a>.", linkUrlResolver);

            Assert.Equal("Learn <a href=\"\" data-item-id=\"OTHER\">more</a>.", result);
        }

        [Fact]
        public void ExternalLinksArePreserved()
        {
            var result = ResolveContentLinks("Learn <a href=\"https://www.kentico.com\">more</a>.");

            Assert.Equal("Learn <a href=\"https://www.kentico.com\">more</a>.", result);
        }

        [Fact]
        public void ExternalEmptyLinksArePreserved()
        {
            var result = ResolveContentLinks("Learn <a href=\"\">more</a>.");

            Assert.Equal("Learn <a href=\"\">more</a>.", result);
        }

        [Fact]
        public void UrlLinkIsEncoded()
        {
            var linkUrlResolver = new CustomContentLinkUrlResolver
            {
                GetLinkUrl = link => "http://example.org?q=bits&bolts"
            };
            var result = ResolveContentLinks("Learn <a href=\"\" data-item-id=\"CID\">more</a>.", linkUrlResolver);

            Assert.Equal("Learn <a href=\"http://example.org?q=bits&amp;bolts\" data-item-id=\"CID\">more</a>.", result);
        }

        [Fact]
        public void BrokenUrlLinkIsEncoded()
        {
            var linkUrlResolver = new CustomContentLinkUrlResolver
            {
                GetBrokenLinkUrl = () => "http://example.org/<broken>"
            };
            var result = ResolveContentLinks("Learn <a href=\"\" data-item-id=\"OTHER\">more</a>.", linkUrlResolver);

            Assert.Equal("Learn <a href=\"http://example.org/&lt;broken&gt;\" data-item-id=\"OTHER\">more</a>.", result);
        }

        [Fact]
        public void ContentLinkAttributesAreParsed()
        {
            var linkUrlResolver = new CustomContentLinkUrlResolver
            {
                GetLinkUrl = link => $"http://example.org/{link.ContentTypeCodename}/{link.Codename}/{link.Id}-{link.UrlSlug}"
            };
            var result = ResolveContentLinks("Learn <a href=\"\" data-item-id=\"CID\">more</a>.", linkUrlResolver);

            Assert.Equal("Learn <a href=\"http://example.org/article/about_us/CID-about-us\" data-item-id=\"CID\">more</a>.", result);
        }

        [Fact]
        public async void ResolveLinksInStronglyTypedModel()
        {
            var mockHttp = new MockHttpMessageHandler();
            string guid = Guid.NewGuid().ToString();
            string url = $"https://deliver.kenticocloud.com/{guid}/items/coffee_processing_techniques";

            mockHttp.When(url).
               Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures\\ContentLinkResolver\\coffee_processing_techniques.json")));

            var httpClient = mockHttp.ToHttpClient();
            DeliveryClient client = new DeliveryClient(guid)
            {
                CodeFirstModelProvider = {TypeProvider = new CustomTypeProvider()},
                HttpClient = httpClient,
                ContentLinkUrlResolver = new CustomContentLinkUrlResolver()
            };


            string expected = "Check out our <a data-item-id=\"0c9a11bb-6fc3-409c-b3cb-f0b797e15489\" href=\"http://example.org/brazil-natural-barra-grande\">Brazil Natural Barra Grande</a> coffee for a tasty example.";
            var item = await client.GetItemAsync<Article>("coffee_processing_techniques");

            Assert.Contains(expected, item.Item.BodyCopy);
        }

        private string ResolveContentLinks(string text)
        {
            var linkUrlResolver = new CustomContentLinkUrlResolver();

            return ResolveContentLinks(text, linkUrlResolver);
        }

        private string ResolveContentLinks(string text, CustomContentLinkUrlResolver linkUrlResolver)
        {
            var linkResolver = new ContentLinkResolver(linkUrlResolver);
            var links = JObject.FromObject(new
            {
                CID = new
                {
                    codename = "about_us",
                    type = "article",
                    url_slug = "about-us"
                }
            });

            return linkResolver.ResolveContentLinks(text, links);
        }

        private sealed class CustomContentLinkUrlResolver : IContentLinkUrlResolver
        {
            public Func<ContentLink, string> GetLinkUrl = link => $"http://example.org/{link.UrlSlug}";
            public Func<string> GetBrokenLinkUrl = () => "http://example.org/broken";

            public string ResolveLinkUrl(ContentLink link)
            {
                return GetLinkUrl(link);
            }

            public string ResolveBrokenLinkUrl()
            {
                return GetBrokenLinkUrl();
            }
        }
    }
}
