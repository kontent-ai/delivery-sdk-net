using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;

namespace KenticoCloud.Delivery.Tests
{
    [TestFixture]
    public class ContentLinkResolverTests
    {
        [Test]
        public void ContentLinkIsResolved()
        {
            var result = ResolveContentLinks("Learn <a href=\"\" data-item-id=\"CID\">more</a>.");

            Assert.AreEqual("Learn <a href=\"http://example.org/about-us\" data-item-id=\"CID\">more</a>.", result);
        }

        [Test]
        public void DecoratedContentLinkIsResolved()
        {
            var result = ResolveContentLinks("Learn <a href=\"\" data-item-id=\"CID\" class=\"link\">more</a>.");

            Assert.AreEqual("Learn <a href=\"http://example.org/about-us\" data-item-id=\"CID\" class=\"link\">more</a>.", result);
        }

        [Test]
        public void BrokenContentLinkIsNotResolved()
        {
            var result = ResolveContentLinks("Learn <a href=\"\" data-item-id=\"OTHER\">more</a>.");

            Assert.AreEqual("Learn <a href=\"\" data-item-id=\"OTHER\">more</a>.", result);
        }

        [Test]
        public void ResolveLinkUrlIsOptional()
        {
            var linkUrlResolver = new CustomContentLinkUrlResolver
            {
                GetLinkUrl = (link, context) => null
            };
            var result = ResolveContentLinks("Learn <a href=\"\" data-item-id=\"CID\">more</a>.", linkUrlResolver);

            Assert.AreEqual("Learn <a href=\"\" data-item-id=\"CID\">more</a>.", result);
        }

        [Test]
        public void ExternalLinksArePreserved()
        {
            var result = ResolveContentLinks("Learn <a href=\"https://www.kentico.com\">more</a>.");

            Assert.AreEqual("Learn <a href=\"https://www.kentico.com\">more</a>.", result);
        }

        [Test]
        public void ExternalEmptyLinksArePreserved()
        {
            var result = ResolveContentLinks("Learn <a href=\"\">more</a>.");

            Assert.AreEqual("Learn <a href=\"\">more</a>.", result);
        }

        [Test]
        public void UrlLinkIsEncoded()
        {
            var linkUrlResolver = new CustomContentLinkUrlResolver
            {
                GetLinkUrl = (link, context) => "http://example.org?q=bits&bolts"
            };
            var result = ResolveContentLinks("Learn <a href=\"\" data-item-id=\"CID\">more</a>.", linkUrlResolver);

            Assert.AreEqual("Learn <a href=\"http://example.org?q=bits&amp;bolts\" data-item-id=\"CID\">more</a>.", result);
        }

        [Test]
        public void ContentLinkAttributesAreParsed()
        {
            var linkUrlResolver = new CustomContentLinkUrlResolver
            {
                GetLinkUrl = (link, context) => $"http://example.org/{link.ContentTypeCodename}/{link.Codename}/{link.Id}-{link.UrlSlug}"
            };
            var result = ResolveContentLinks("Learn <a href=\"\" data-item-id=\"CID\">more</a>.", linkUrlResolver);

            Assert.AreEqual("Learn <a href=\"http://example.org/article/about_us/CID-about-us\" data-item-id=\"CID\">more</a>.", result);
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

            return linkResolver.ResolveContentLinks(text, links, new ContentLinkUrlResolverContext());
        }

        private sealed class CustomContentLinkUrlResolver : IContentLinkUrlResolver
        {
            public Func<ContentLink, ContentLinkUrlResolverContext, string> GetLinkUrl = (link, context) => $"http://example.org/{link.UrlSlug}";

            public string ResolveLinkUrl(ContentLink link, ContentLinkUrlResolverContext context)
            {
                return GetLinkUrl(link, context);
            }
        }
    }
}
