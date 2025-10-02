using System;
using System.IO;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.Extensions;
using Kontent.Ai.Delivery.Tests.Factories;
using Microsoft.Extensions.DependencyInjection;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;
using RichardSzalay.MockHttp;
using Xunit;
using Kontent.Ai.Delivery.ContentItems.RichText.Resolution;

namespace Kontent.Ai.Delivery.Tests;

public class ContentLinkResolverTests
{

    [Fact]
    public async Task ResolveLinksInStronglyTypedModel()
    {
        var mockHttp = new MockHttpMessageHandler();
        string guid = Guid.NewGuid().ToString();
        string url = $"https://deliver.kontent.ai/{guid}/items/coffee_processing_techniques";
        mockHttp.When(url)
            .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}ContentLinkResolver{Path.DirectorySeparatorChar}coffee_processing_techniques.json")));

        var services = new ServiceCollection();
        services.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = guid },
            configureRefit: null,
            configureHttpClient: builder => builder.ConfigurePrimaryHttpMessageHandler(() => mockHttp));

        // Override services to use custom implementations
        services.AddSingleton<IContentLinkUrlResolver, CustomContentLinkUrlResolver>();
        services.AddSingleton<ITypeProvider, CustomTypeProvider>();

        var provider = services.BuildServiceProvider();
        var client = (DeliveryClient)provider.GetRequiredService<IDeliveryClient>();

        var result = await client.GetItem<Article>("coffee_processing_techniques").ExecuteAsync();

        Assert.True(result.IsSuccess);

        // Use the new structured approach with LegacyUrlResolver
        var urlResolver = provider.GetRequiredService<IContentLinkUrlResolver>();
        var resolver = new Kontent.Ai.Delivery.ContentItems.RichText.Resolution.HtmlResolverBuilder()
            .WithDefaultResolvers()
            .WithContentItemLinkResolver(DefaultResolvers.LegacyUrlResolver(urlResolver))
            .Build();

        var html = await result.Value.Elements.BodyCopy.ToHtmlAsync(resolver);

        // Check that the link is resolved correctly (attribute order may vary)
        Assert.Contains("Check out our", html);
        Assert.Contains("Brazil Natural Barra Grande", html);
        Assert.Contains("href=\"http://example.org/brazil-natural-barra-grande\"", html);
        Assert.Contains("data-item-id=\"0c9a11bb-6fc3-409c-b3cb-f0b797e15489\"", html);
        Assert.Contains("coffee for a tasty example", html);
    }

    private sealed class CustomContentLinkUrlResolver : IContentLinkUrlResolver
    {
        public Func<IContentLink, string> GetLinkUrl = link => $"http://example.org/{link.UrlSlug}";
        public Func<string> GetBrokenLinkUrl = () => "http://example.org/broken";

        public Task<string> ResolveBrokenLinkUrlAsync()
        {
            return Task.FromResult(GetBrokenLinkUrl());
        }

        public Task<string> ResolveLinkUrlAsync(IContentLink link)
        {
            return Task.FromResult(GetLinkUrl(link));
        }
    }
}
