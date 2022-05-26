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
    public void GetLanguagesUrl_ConstructedWithDeliveryOptions_GetsLanguagesUrl()
    {
        var options = new DeliveryOptions() { ProjectId = Guid.NewGuid().ToString() };

        var deliveryEndpointUrlBuilder = new DeliveryEndpointUrlBuilder(options);

        var actualLanguagesUrl = deliveryEndpointUrlBuilder.GetLanguagesUrl(new IQueryParameter[] { });

        var expectedLanguagesUrl = $"https://deliver.kontent.ai:443/{options.ProjectId}/languages";
        Assert.Equal(expectedLanguagesUrl, actualLanguagesUrl);
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