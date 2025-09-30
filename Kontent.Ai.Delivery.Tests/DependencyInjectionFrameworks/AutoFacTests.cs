using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Tests.DependencyInjectionFrameworks.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.DependencyInjectionFrameworks;

/// <summary>
/// Smoke test to verify SDK works with AutoFac container.
/// If this works with MS.DI (which we test extensively), it works with all containers.
/// </summary>
[Collection("DI Tests")]
public class AutoFacTests
{
    [Fact]
    public void DeliveryClient_CanBeResolvedFromAutoFacContainer()
    {
        // Arrange
        var services = DependencyInjectionFrameworksHelper.GetServiceCollection();
        var provider = services.BuildAutoFacServiceProvider();

        // Act - Verify all critical services can be resolved
        var client = provider.GetRequiredService<IDeliveryClient>();
        var typeProvider = provider.GetRequiredService<ITypeProvider>();
        var typingStrategy = provider.GetRequiredService<IItemTypingStrategy>();
        var deserializer = provider.GetRequiredService<IContentDeserializer>();

        // Assert
        Assert.NotNull(client);
        Assert.IsType<DeliveryClient>(client);
        Assert.NotNull(typeProvider);
        Assert.NotNull(typingStrategy);
        Assert.NotNull(deserializer);
    }
}
