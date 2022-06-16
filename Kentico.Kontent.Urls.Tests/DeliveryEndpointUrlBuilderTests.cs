using System;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Urls.Delivery;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kentico.Kontent.Urls.Tests;

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
    public void GetItemUrl_ConstructedWithDeliveryOptions_GetsItemUrl()
    {
        var options = new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() };
        var deliveryEndpointUrlBuilder = new DeliveryEndpointUrlBuilder(options);

        var actualItemUrl = deliveryEndpointUrlBuilder.GetItemUrl("item_codename",new IQueryParameter[] { });

        var expectedItemUrl = $"https://deliver.kontent.ai:443/{options.ProjectId}/items/item_codename";
        Assert.Equal(expectedItemUrl, actualItemUrl);
    }
    
    [Fact]
    public void GetItemUrl_ConstructedWithDeliveryOptionsMonitor_GetsItemUrl()
    {
        var options = new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() };
        var optionsMonitor = new FakeOptionsMonitor<DeliveryOptions>(options);
        var deliveryEndpointUrlBuilder = new DeliveryEndpointUrlBuilder(optionsMonitor);

        var actualItemUrl = deliveryEndpointUrlBuilder.GetItemUrl("item_codename", new IQueryParameter[] { });

        var expectedItemUrl = $"https://deliver.kontent.ai:443/{options.ProjectId}/items/item_codename";
        Assert.Equal(expectedItemUrl, actualItemUrl);
    }
    
    [Fact]
    public void GetItemsUrl_ConstructedWithDeliveryOptionsMonitor_GetsItemsUrl()
    {
        var options = new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() };
        var optionsMonitor = new FakeOptionsMonitor<DeliveryOptions>(options);
        var deliveryEndpointUrlBuilder = new DeliveryEndpointUrlBuilder(optionsMonitor);

        var actualItemsUrl = deliveryEndpointUrlBuilder.GetItemsUrl(new IQueryParameter[] { });

        var expectedItemsUrl = $"https://deliver.kontent.ai:443/{options.ProjectId}/items";
        Assert.Equal(expectedItemsUrl, actualItemsUrl);
    }
    
    [Fact]
    public void GetItemsFeedUrl_ConstructedWithDeliveryOptionsMonitor_GetsItemsFeedUrl()
    {
        var options = new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() };
        var optionsMonitor = new FakeOptionsMonitor<DeliveryOptions>(options);
        var deliveryEndpointUrlBuilder = new DeliveryEndpointUrlBuilder(optionsMonitor);

        var actualItemsFeedUrl = deliveryEndpointUrlBuilder.GetItemsFeedUrl(new IQueryParameter[] { });

        var expectedItemsFeedUrl = $"https://deliver.kontent.ai:443/{options.ProjectId}/items-feed";
        Assert.Equal(expectedItemsFeedUrl, actualItemsFeedUrl);
    }
    
    [Fact]
    public void GetContentElementUrl_ConstructedWithDeliveryOptionsMonitor_GetsContentElementUrl()
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
    public void GetTaxonomyUrl_ConstructedWithDeliveryOptionsMonitor_GetsTaxonomyUrl()
    {
        var options = new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() };
        var optionsMonitor = new FakeOptionsMonitor<DeliveryOptions>(options);
        var deliveryEndpointUrlBuilder = new DeliveryEndpointUrlBuilder(optionsMonitor);

        var actualTaxonomyUrl = deliveryEndpointUrlBuilder.GetTaxonomyUrl("taxonomy_codename");

        var expectedTaxonomyUrl = $"https://deliver.kontent.ai:443/{options.ProjectId}/taxonomies/taxonomy_codename";
        Assert.Equal(expectedTaxonomyUrl, actualTaxonomyUrl);
    }
    
    [Fact]
    public void GetTaxonomiesUrl_ConstructedWithDeliveryOptionsMonitor_GetsTaxonomiesUrl()
    {
        var options = new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() };
        var optionsMonitor = new FakeOptionsMonitor<DeliveryOptions>(options);
        var deliveryEndpointUrlBuilder = new DeliveryEndpointUrlBuilder(optionsMonitor);

        var actualTaxonomiesUrl = deliveryEndpointUrlBuilder.GetTaxonomiesUrl(new IQueryParameter[] { });

        var expectedTaxonomiesUrl = $"https://deliver.kontent.ai:443/{options.ProjectId}/taxonomies";
        Assert.Equal(expectedTaxonomiesUrl, actualTaxonomiesUrl);
    }
    
    [Fact]
    public void GetLanguagesUrl_ConstructedWithDeliveryOptionsMonitor_GetsLanguagesUrl()
    {
        var options = new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() };
        var optionsMonitor = new FakeOptionsMonitor<DeliveryOptions>(options);
        var deliveryEndpointUrlBuilder = new DeliveryEndpointUrlBuilder(optionsMonitor);

        var actualLanguagesUrl = deliveryEndpointUrlBuilder.GetLanguagesUrl(new IQueryParameter[] { });

        var expectedLanguagesUrl = $"https://deliver.kontent.ai:443/{options.ProjectId}/languages";
        Assert.Equal(expectedLanguagesUrl, actualLanguagesUrl);
    }
}