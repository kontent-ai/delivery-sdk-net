using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Kontent.Ai.Delivery.Tests.DependencyInjectionFrameworks.Helpers;

/// <summary>
/// Helper for testing SDK compatibility with AutoFac DI container.
/// </summary>
internal static class DependencyInjectionFrameworksHelper
{
    private const string EnvironmentId = "00a21be4-8fef-4dd9-9380-f4cbb82e260d";

    internal static IServiceCollection GetServiceCollection()
        => new ServiceCollection()
            .AddDeliveryClient(new DeliveryOptions { EnvironmentId = EnvironmentId });

    internal static IServiceProvider BuildAutoFacServiceProvider(this IServiceCollection serviceCollection)
    {
        var autoFacContainerBuilder = new ContainerBuilder();
        autoFacContainerBuilder.Populate(serviceCollection);
        var container = autoFacContainerBuilder.Build();
        return new AutofacServiceProvider(container);
    }
}
