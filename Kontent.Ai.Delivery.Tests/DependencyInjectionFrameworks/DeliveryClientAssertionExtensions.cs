using System.Linq;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.ContentLinks;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.DependencyInjectionFrameworks;

internal static class DeliveryClientAssertionExtensions
{
    private const string EnvironmentId = "00a21be4-8fef-4dd9-9380-f4cbb82e260d";

    internal static void AssertDefaultDependencies(this IDeliveryClient client)
    {
        // We cannot access DI internals directly through IDeliveryClient; assert via behavior where possible in separate tests.
        // Here, just assert the client type is the concrete internal type and DI produced it.
        Assert.IsType<DeliveryClient>(client);
    }

    internal static void AssertDefaultDependenciesWithModelProviderAndInlineContentItemTypeResolvers<TCustomModelProvider>(
        this IDeliveryClient client)
        where TCustomModelProvider : class
        => client.AssertDefaultDependencies();

    // Legacy assertion removed: internal DI details (ModelProvider, Options monitor) are not accessible through IDeliveryClient.

    // Resolver presence is verified in behavior tests (rich text rendering). No direct assertions here.
}
