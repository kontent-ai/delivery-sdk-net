﻿using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Tests.DependencyInjectionFrameworks.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.DependencyInjectionFrameworks
{
    [Collection("DI Tests")]
    public class UnityTests
    {
        [Fact]
        public void DeliveryClientIsSuccessfullyResolvedFromUnityContainer()
        {
            var provider = DependencyInjectionFrameworksHelper
                .GetServiceCollection()
                .RegisterInlineContentItemResolvers()
                .BuildUnityServiceProvider();

            var client = (DeliveryClient)provider.GetRequiredService<IDeliveryClient>();

            client.AssertDefaultDependencies();
        }

        [Fact]
        public void DeliveryClientIsSuccessfullyResolvedFromUnityContainer_CustomModelProvider()
        {
            var provider = DependencyInjectionFrameworksHelper
                .GetServiceCollection()
                .AddSingleton<IModelProvider, FakeModelProvider>()
                .BuildUnityServiceProvider();

            var client = (DeliveryClient)provider.GetRequiredService<IDeliveryClient>();

            client.AssertDefaultDependenciesWithModelProviderAndInlineContentItemTypeResolvers<FakeModelProvider>();
        }
    }
}
