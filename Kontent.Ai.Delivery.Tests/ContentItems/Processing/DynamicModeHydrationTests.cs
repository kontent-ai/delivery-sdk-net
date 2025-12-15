using System.Text.Json;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Extensions;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.ContentItems.Processing;

public sealed class DynamicModeHydrationTests
{
    private readonly Guid _guid = Guid.NewGuid();
    private string BaseUrl => $"https://deliver.kontent.ai/{_guid}";

    private IDeliveryClient CreateClient(MockHttpMessageHandler mockHttp)
    {
        var services = new ServiceCollection();
        var options = new DeliveryOptions { EnvironmentId = _guid.ToString() };
        services.AddDeliveryClient(options, configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mockHttp));
        return services.BuildServiceProvider().GetRequiredService<IDeliveryClient>();
    }

    [Fact]
    public async Task DynamicMode_ItemQuery_PreservesRawElementEnvelopeStructure()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var fixtureContent = await File.ReadAllTextAsync(
            Path.Combine(Environment.CurrentDirectory,
                $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json"));

        mock.When($"{BaseUrl}/items/coffee_beverages_explained")
            .Respond("application/json", fixtureContent);

        var client = CreateClient(mock);

        // Act
        var result = await client.GetItem<IDynamicElements>("coffee_beverages_explained").ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Elements);

        Assert.True(result.Value.Elements.TryGetValue("title", out var titleEnvelope));
        Assert.Equal(JsonValueKind.Object, titleEnvelope.ValueKind);

        Assert.True(titleEnvelope.TryGetProperty("type", out var typeEl));
        Assert.True(titleEnvelope.TryGetProperty("name", out var nameEl));
        Assert.True(titleEnvelope.TryGetProperty("value", out var valueEl));

        Assert.Equal("text", typeEl.GetString());
        Assert.Equal("Title", nameEl.GetString());
        Assert.Equal("Coffee Beverages Explained", valueEl.GetString());
    }
}


