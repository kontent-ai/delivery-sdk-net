using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.ContentItems;
using Kentico.Kontent.Delivery.ContentItems.ContentLinks;
using Kentico.Kontent.Delivery.RetryPolicy;
using Kentico.Kontent.Delivery.Tests.Factories;
using Kentico.Kontent.Delivery.Tests.Models.ContentTypes;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests
{
    public class ContentLinkResolverTests
    {
        private readonly Guid ContentItemIdA = Guid.NewGuid();
        private readonly Guid ContentItemIdB = Guid.NewGuid();

        [Fact]
        public async Task ContentLinkIsResolved()
        {
            var result = await ResolveContentLinks($"Learn <a href=\"\" data-item-id=\"{ContentItemIdA}\">more</a>.");

            Assert.Equal($"Learn <a href=\"http://example.org/about-us\" data-item-id=\"{ContentItemIdA}\">more</a>.", result);
        }

        [Fact]
        public async Task DecoratedContentLinkIsResolved()
        {
            var result = await ResolveContentLinks($"Learn <a href=\"\" data-item-id=\"{ContentItemIdA}\" class=\"link\">more</a>.");

            Assert.Equal($"Learn <a href=\"http://example.org/about-us\" data-item-id=\"{ContentItemIdA}\" class=\"link\">more</a>.", result);
        }

        [Fact]
        public async Task BrokenContentLinkIsResolved()
        {
            var result = await ResolveContentLinks($"Learn <a href=\"\" data-item-id=\"{ContentItemIdB}\">more</a>.");

            Assert.Equal($"Learn <a href=\"http://example.org/broken\" data-item-id=\"{ContentItemIdB}\">more</a>.", result);
        }

        [Fact]
        public async Task ResolveLinkUrlIsOptional()
        {
            var linkUrlResolver = new CustomContentLinkUrlResolver
            {
                GetLinkUrl = link => null
            };
            var result = await ResolveContentLinks($"Learn <a href=\"\" data-item-id=\"{ContentItemIdA}\">more</a>.", linkUrlResolver);

            Assert.Equal($"Learn <a href=\"\" data-item-id=\"{ContentItemIdA}\">more</a>.", result);
        }

        [Fact]
        public async Task ResolveBrokenLinkUrlIsOptional()
        {
            var linkUrlResolver = new CustomContentLinkUrlResolver
            {
                GetBrokenLinkUrl = () => null
            };
            var result = await ResolveContentLinks($"Learn <a href=\"\" data-item-id=\"{ContentItemIdB}\">more</a>.", linkUrlResolver);

            Assert.Equal($"Learn <a href=\"\" data-item-id=\"{ContentItemIdB}\">more</a>.", result);
        }

        [Fact]
        public async Task ExternalLinksArePreserved()
        {
            var result = await ResolveContentLinks("Learn <a href=\"https://www.kentico.com\">more</a>.");

            Assert.Equal("Learn <a href=\"https://www.kentico.com\">more</a>.", result);
        }

        [Fact]
        public async Task ExternalEmptyLinksArePreserved()
        {
            var result = await ResolveContentLinks("Learn <a href=\"\">more</a>.");

            Assert.Equal("Learn <a href=\"\">more</a>.", result);
        }

        [Fact]
        public async Task UrlLinkIsEncoded()
        {
            var linkUrlResolver = new CustomContentLinkUrlResolver
            {
                GetLinkUrl = link => "http://example.org?q=bits&bolts"
            };
            var result = await ResolveContentLinks($"Learn <a href=\"\" data-item-id=\"{ContentItemIdA}\">more</a>.", linkUrlResolver);

            Assert.Equal($"Learn <a href=\"http://example.org?q=bits&amp;bolts\" data-item-id=\"{ContentItemIdA}\">more</a>.", result);
        }

        [Fact]
        public async Task BrokenUrlLinkIsEncoded()
        {
            var linkUrlResolver = new CustomContentLinkUrlResolver
            {
                GetBrokenLinkUrl = () => "http://example.org/<broken>"
            };
            var result = await ResolveContentLinks($"Learn <a href=\"\" data-item-id=\"{ContentItemIdB}\">more</a>.", linkUrlResolver);

            Assert.Equal($"Learn <a href=\"http://example.org/&lt;broken&gt;\" data-item-id=\"{ContentItemIdB}\">more</a>.", result);
        }

        [Fact]
        public async Task ContentLinkAttributesAreParsed()
        {
            var linkUrlResolver = new CustomContentLinkUrlResolver
            {
                GetLinkUrl = link => $"http://example.org/{link.ContentTypeCodename}/{link.Codename}/{link.Id}-{link.UrlSlug}"
            };
            var result = await ResolveContentLinks($"Learn <a href=\"\" data-item-id=\"{ContentItemIdA}\">more</a>.", linkUrlResolver);

            Assert.Equal($"Learn <a href=\"http://example.org/article/about_us/{ContentItemIdA}-about-us\" data-item-id=\"{ContentItemIdA}\">more</a>.", result);
        }

        [Fact]
        public async void ResolveLinksInStronglyTypedModel()
        {
            var mockHttp = new MockHttpMessageHandler();
            string guid = Guid.NewGuid().ToString();
            string url = $"https://deliver.kontent.ai/{guid}/items/coffee_processing_techniques";
            mockHttp.When(url).
               Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}ContentLinkResolver{Path.DirectorySeparatorChar}coffee_processing_techniques.json")));

            var deliveryOptions = DeliveryOptionsFactory.CreateMonitor(new DeliveryOptions { ProjectId = guid });
            var options = DeliveryOptionsFactory.Create(new DeliveryOptions { ProjectId = guid });
            var deliveryHttpClient = new DeliveryHttpClient(mockHttp.ToHttpClient());
            var resiliencePolicyProvider = new DefaultRetryPolicyProvider(options);
            var contentLinkUrlResolver = new CustomContentLinkUrlResolver();
            var contentItemsProcessor = InlineContentItemsProcessorFactory.Create();
            var modelProvider = new ModelProvider(contentLinkUrlResolver, contentItemsProcessor, new CustomTypeProvider(), new PropertyMapper(), new DeliveryJsonSerializer(), new HtmlParser());
            var client = new DeliveryClient(
                deliveryOptions,
                modelProvider,
                resiliencePolicyProvider,
                null,
                deliveryHttpClient
            );

            string expected = "Check out our <a data-item-id=\"0c9a11bb-6fc3-409c-b3cb-f0b797e15489\" href=\"http://example.org/brazil-natural-barra-grande\">Brazil Natural Barra Grande</a> coffee for a tasty example.";
            var item = await client.GetItemAsync<Article>("coffee_processing_techniques");

            Assert.Contains(expected, item.Item.BodyCopy);
        }

        private async Task<string> ResolveContentLinks(string text)
        {
            var linkUrlResolver = new CustomContentLinkUrlResolver();
            return await ResolveContentLinks(text, linkUrlResolver);
        }

        private async Task<string> ResolveContentLinks(string text, CustomContentLinkUrlResolver linkUrlResolver)
        {
            var linkResolver = new ContentLinkResolver(linkUrlResolver);
            IContentLink link = new ContentLink()
            {
                ContentTypeCodename = "article",
                UrlSlug = "about-us",
                Codename = "about_us"
            };

            link.Id = ContentItemIdA;
            var links = new Dictionary<Guid, IContentLink> { { ContentItemIdA, link } };

            return await linkResolver.ResolveContentLinks(text, links);
        }

        private sealed class CustomContentLinkUrlResolver : IContentLinkUrlResolver
        {
            public Func<IContentLink, string> GetLinkUrl = link => $"http://example.org/{link.UrlSlug}";
            public Func<string> GetBrokenLinkUrl = () => "http://example.org/broken";

            public Task<string> ResolveBrokenLinkUrl()
            {
                return Task.FromResult(GetBrokenLinkUrl());
            }

            public Task<string> ResolveLinkUrl(IContentLink link)
            {
                return Task.FromResult(GetLinkUrl(link));
            }
        }
    }
}
