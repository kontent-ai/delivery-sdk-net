using Kontent.Ai.Delivery.Configuration;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Builders.Configuration;

public class DeliveryOptionsBuilderTests
{
    private const string EnvironmentId = "550cec62-90a6-4ab3-b3e4-3d0bb4c04f5c";
    private const string PreviewApiKey =
        "eyJ0eXAiOiwq14X65DLCJhbGciOiJIUzI1NiJ-.eyJqdGkiOiABCjJlM2FiOTBjOGM0ODVmYjdmZTDEFRQZGM1NDIyMCIsImlhdCI6IjE1Mjg454wexiLCJleHAiOiIxODc0NDg3NjqasdfwicHJvamVjdF9pZCI6Ij" +
        "g1OTEwOTlkN2458198ewqewZjI3Yzg5M2FhZTJiNTE4IiwidmVyIjoiMS4wLjAiLCJhdWQiewqgsdaWV3LmRlbGl2ZXIua2VudGljb2Nsb3VkLmNvbSJ9._tSzbNDpbE55dsaLUTGsdgesg4b693TFuhRCRsDyoc";

    [Fact]
    public void BuildWithDisabledRetryLogic()
    {
        var deliveryOptions = DeliveryOptionsBuilder
            .CreateInstance()
            .WithEnvironmentId(Guid.NewGuid())
            .UseProductionApi()
            .DisableRetryPolicy()
            .Build();

        Assert.False(deliveryOptions.EnableResilience);
    }

    [Fact]
    public void ModernBuilder_WithWaitForLoadingNewContent_SetsOptions()
    {
        var deliveryOptions = DeliveryOptionsBuilder
            .CreateInstance()
            .WithEnvironmentId(EnvironmentId)
            .UseProductionApi()
            .WaitForLoadingNewContent()
            .Build();

        Assert.True(deliveryOptions.WaitForLoadingNewContent);
    }

    [Fact]
    public void BuildWithCustomEndpointForPreviewApi()
    {
        const string customEndpoint = "http://www.customPreviewEndpoint.com";

        var deliveryOptions = DeliveryOptionsBuilder
            .CreateInstance()
            .WithEnvironmentId(EnvironmentId)
            .UsePreviewApi(PreviewApiKey)
            .WithCustomEndpoint(customEndpoint)
            .Build();

        Assert.Equal(customEndpoint, deliveryOptions.PreviewEndpoint);
    }

    [Fact]
    public void BuildWithCustomEndpointForProductionApi()
    {
        const string customEndpoint = "https://www.customProductionEndpoint.com";

        var deliveryOptions = DeliveryOptionsBuilder
            .CreateInstance()
            .WithEnvironmentId(EnvironmentId)
            .UseProductionApi()
            .WithCustomEndpoint(customEndpoint)
            .Build();

        Assert.Equal(customEndpoint, deliveryOptions.ProductionEndpoint);
    }

    [Fact]
    public void BuildWithCustomEndpointAsUriForPreviewApi()
    {
        const string customEndpoint = "http://www.custompreviewendpoint.com/";
        var uri = new Uri(customEndpoint, UriKind.Absolute);

        var deliveryOptions = DeliveryOptionsBuilder
            .CreateInstance()
            .WithEnvironmentId(EnvironmentId)
            .UsePreviewApi(PreviewApiKey)
            .WithCustomEndpoint(uri)
            .Build();

        Assert.Equal(customEndpoint, deliveryOptions.PreviewEndpoint);
    }

    [Fact]
    public void BuildWithCustomEndpointAsUriForProductionApi()
    {
        const string customEndpoint = "https://www.customproductionendpoint.com/";
        var uri = new Uri(customEndpoint, UriKind.Absolute);

        var deliveryOptions = DeliveryOptionsBuilder
            .CreateInstance()
            .WithEnvironmentId(EnvironmentId)
            .UseProductionApi()
            .WithCustomEndpoint(uri)
            .Build();

        Assert.Equal(customEndpoint, deliveryOptions.ProductionEndpoint);
    }

    [Fact]
    public void BuildWithDefaultRenditionPreset()
    {
        const string renditionPreset = "mobile";

        var deliveryOptions = DeliveryOptionsBuilder
            .CreateInstance()
            .WithEnvironmentId(EnvironmentId)
            .UseProductionApi()
            .WithDefaultRenditionPreset(renditionPreset)
            .Build();

        Assert.Equal(renditionPreset, deliveryOptions.DefaultRenditionPreset);
    }
}