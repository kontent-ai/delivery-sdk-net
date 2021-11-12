﻿using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.ContentItems;
using Kentico.Kontent.Delivery.Tests.DependencyInjectionFrameworks.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests.DependencyInjectionFrameworks
{
    [Collection("DI Tests")]
    public class AutoFacTests
    {
        [Fact]
        public void DeliveryClientIsSuccessfullyResolvedFromAutoFacContainer()
        {
            var provider = DependencyInjectionFrameworksHelper
                .GetServiceCollection()
                .RegisterInlineContentItemResolvers()
                .BuildAutoFacServiceProvider();

            var client = (DeliveryClient)provider.GetRequiredService<IDeliveryClient>();

            client.AssertDefaultDependencies();
        }
        
        [Fact]
        public void DeliveryClientIsSuccessfullyResolvedFromCastleWindsorContainer_CustomModelProvider()
        {
            var provider = DependencyInjectionFrameworksHelper
                .GetServiceCollection()
                .AddSingleton<IModelProvider, FakeModelProvider>()
                .BuildAutoFacServiceProvider();

            var client = (DeliveryClient)provider.GetRequiredService<IDeliveryClient>();

            client.AssertDefaultDependenciesWithModelProviderAndInlineContentItemTypeResolvers<FakeModelProvider>();
        }
    }
}
