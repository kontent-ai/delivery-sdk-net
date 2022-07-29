using System;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Urls.Delivery;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kontent.Ai.Urls.Tests;

public class DeliveryEndpointUrlBuilderTests
{
    //as this is only place where mock would be used, just fake implementation class is created
    //if more mocking is to be used, please consider usage mocking library package like NSubstitute instead
    private class FakeOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public T CurrentValue { get; }

        public FakeOptionsMonitor(T options)
        {
            CurrentValue = options;
        }
        
        public T Get(string name)
        {
            throw new NotImplementedException();
        }

        public IDisposable OnChange(Action<T, string> listener)
        {
            throw new NotImplementedException();
        }
    }
        
    [Fact]
    public void GetItemUrl_ConstructedWithDeliveryOptionsMonitor_ReturnsItemUrl()
    {
        var options = new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() };
        var optionsMonitor = new FakeOptionsMonitor<DeliveryOptions>(options);
        var deliveryEndpointUrlBuilder = new DeliveryEndpointUrlBuilder(optionsMonitor);

        var actualItemUrl = deliveryEndpointUrlBuilder.GetItemUrl("item_codename", new IQueryParameter[] { });

        var expectedItemUrl = $"https://deliver.kontent.ai:443/{options.ProjectId}/items/item_codename";
        Assert.Equal(expectedItemUrl, actualItemUrl);
    }
    
    [Fact]
    public void GetItemsUrl_ReturnsItemsUrl()
    {
        var options = new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() };
        var optionsMonitor = new FakeOptionsMonitor<DeliveryOptions>(options);
        var deliveryEndpointUrlBuilder = new DeliveryEndpointUrlBuilder(optionsMonitor);

        var actualItemsUrl = deliveryEndpointUrlBuilder.GetItemsUrl(new IQueryParameter[] { });

        var expectedItemsUrl = $"https://deliver.kontent.ai:443/{options.ProjectId}/items";
        Assert.Equal(expectedItemsUrl, actualItemsUrl);
    }
    
    [Fact]
    public void GetItemsFeedUrl_ReturnsItemsFeedUrl()
    {
        var options = new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() };
        var optionsMonitor = new FakeOptionsMonitor<DeliveryOptions>(options);
        var deliveryEndpointUrlBuilder = new DeliveryEndpointUrlBuilder(optionsMonitor);

        var actualItemsFeedUrl = deliveryEndpointUrlBuilder.GetItemsFeedUrl(new IQueryParameter[] { });

        var expectedItemsFeedUrl = $"https://deliver.kontent.ai:443/{options.ProjectId}/items-feed";
        Assert.Equal(expectedItemsFeedUrl, actualItemsFeedUrl);
    }
    
    [Fact]
    public void GetTypeUrl_ReturnsTypeUrl()
    {
        var options = new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() };
        var optionsMonitor = new FakeOptionsMonitor<DeliveryOptions>(options);
        var deliveryEndpointUrlBuilder = new DeliveryEndpointUrlBuilder(optionsMonitor);

        var actualTypeUrl = deliveryEndpointUrlBuilder.GetTypeUrl("type_codename", new IQueryParameter[] { });

        var expectedTypeUrl = $"https://deliver.kontent.ai:443/{options.ProjectId}/types/type_codename";
        Assert.Equal(expectedTypeUrl, actualTypeUrl);
    }
    
    [Fact]
    public void GetTypesUrl_ReturnsTypesUrl()
    {
        var options = new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() };
        var optionsMonitor = new FakeOptionsMonitor<DeliveryOptions>(options);
        var deliveryEndpointUrlBuilder = new DeliveryEndpointUrlBuilder(optionsMonitor);

        var actualTypesUrl = deliveryEndpointUrlBuilder.GetTypesUrl(new IQueryParameter[] { });

        var expectedTypesUrl = $"https://deliver.kontent.ai:443/{options.ProjectId}/types";
        Assert.Equal(expectedTypesUrl, actualTypesUrl);
    }
    
    [Fact]
    public void GetContentElementUrl_ReturnsContentElementUrl()
    {
        var options = new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() };
        var optionsMonitor = new FakeOptionsMonitor<DeliveryOptions>(options);
        var deliveryEndpointUrlBuilder = new DeliveryEndpointUrlBuilder(optionsMonitor);

        var actualContentElementUrl = deliveryEndpointUrlBuilder
            .GetContentElementUrl("content_type_codename", "content_element_codename");

        var expectedContentElementUrl = $"https://deliver.kontent.ai:443/{options.ProjectId}/types/content_type_codename/elements/content_element_codename";
        Assert.Equal(expectedContentElementUrl, actualContentElementUrl);
    }
    
    [Fact]
    public void GetTaxonomyUrl_ReturnsTaxonomyUrl()
    {
        var options = new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() };
        var optionsMonitor = new FakeOptionsMonitor<DeliveryOptions>(options);
        var deliveryEndpointUrlBuilder = new DeliveryEndpointUrlBuilder(optionsMonitor);

        var actualTaxonomyUrl = deliveryEndpointUrlBuilder.GetTaxonomyUrl("taxonomy_codename");

        var expectedTaxonomyUrl = $"https://deliver.kontent.ai:443/{options.ProjectId}/taxonomies/taxonomy_codename";
        Assert.Equal(expectedTaxonomyUrl, actualTaxonomyUrl);
    }
    
    [Fact]
    public void GetTaxonomiesUrl_ReturnsTaxonomiesUrl()
    {
        var options = new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() };
        var optionsMonitor = new FakeOptionsMonitor<DeliveryOptions>(options);
        var deliveryEndpointUrlBuilder = new DeliveryEndpointUrlBuilder(optionsMonitor);

        var actualTaxonomiesUrl = deliveryEndpointUrlBuilder.GetTaxonomiesUrl(new IQueryParameter[] { });

        var expectedTaxonomiesUrl = $"https://deliver.kontent.ai:443/{options.ProjectId}/taxonomies";
        Assert.Equal(expectedTaxonomiesUrl, actualTaxonomiesUrl);
    }
    
    [Fact]
    public void GetLanguagesUrl_ReturnsLanguagesUrl()
    {
        var options = new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() };
        var optionsMonitor = new FakeOptionsMonitor<DeliveryOptions>(options);
        var deliveryEndpointUrlBuilder = new DeliveryEndpointUrlBuilder(optionsMonitor);

        var actualLanguagesUrl = deliveryEndpointUrlBuilder.GetLanguagesUrl(new IQueryParameter[] { });

        var expectedLanguagesUrl = $"https://deliver.kontent.ai:443/{options.ProjectId}/languages";
        Assert.Equal(expectedLanguagesUrl, actualLanguagesUrl);
    }
}