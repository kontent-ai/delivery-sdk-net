using Kontent.Ai.Delivery.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Configuration;

public class DeliveryClientContainerTests
{
    private const string EnvironmentId = "550cec62-90a6-4ab3-b3e4-3d0bb4c04f5c";

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        using var realContainer = BuildContainer();

        Assert.Throws<ArgumentNullException>(() =>
            new DeliveryClientContainer(null!, realContainer.Client));
    }

    [Fact]
    public void Constructor_NullClient_ThrowsArgumentNullException()
    {
        using var sp = new ServiceCollection().BuildServiceProvider();

        Assert.Throws<ArgumentNullException>(() =>
            new DeliveryClientContainer(sp, null!));
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var container = BuildContainer();

        container.Dispose();
        container.Dispose(); // second call hits early return (line 36)
    }

    [Fact]
    public async Task DisposeAsync_DoesNotThrow()
    {
        var container = BuildContainer();

        await container.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow()
    {
        var container = BuildContainer();

        await container.DisposeAsync();
        await container.DisposeAsync(); // second call hits early return (line 45)
    }

    private static DeliveryClientContainer BuildContainer()
        => (DeliveryClientContainer)DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithEnvironmentId(EnvironmentId)
                .UseProductionApi()
                .Build())
            .Build();
}
