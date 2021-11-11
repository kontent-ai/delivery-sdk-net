﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.Configuration;
using Kentico.Kontent.Delivery.Abstractions.ContentItems;
using Kentico.Kontent.Delivery.Abstractions.ContentItems.ContentLinks;
using Kentico.Kontent.Delivery.Abstractions.ContentItems.InlineContentItems;
using Kentico.Kontent.Delivery.Abstractions.RetryPolicy;
using Kentico.Kontent.Delivery.ContentItems;
using Kentico.Kontent.Delivery.ContentItems.ContentLinks;
using Kentico.Kontent.Delivery.ContentItems.InlineContentItems;
using Kentico.Kontent.Delivery.Extensions;
using Kentico.Kontent.Delivery.RetryPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kentico.Kontent.Delivery.Tests.Extensions
{
    public class ServiceCollectionsExtensionsTests
    {
        private readonly ServiceCollection _serviceCollection;
        private const string ProjectId = "d79786fb-042c-47ec-8e5c-beaf93e38b84";

        private readonly ReadOnlyDictionary<Type, Type> _expectedInterfacesWithImplementationTypes = new ReadOnlyDictionary<Type, Type>(
            new Dictionary<Type, Type>
            {
                { typeof(IContentLinkUrlResolver), typeof(DefaultContentLinkUrlResolver) },
                { typeof(ITypeProvider), typeof(TypeProvider) },
                { typeof(IDeliveryHttpClient), typeof(DeliveryHttpClient) },
                { typeof(IInlineContentItemsProcessor), typeof(InlineContentItemsProcessor) },
                { typeof(IInlineContentItemsResolver<object>), typeof(ReplaceWithWarningAboutRegistrationResolver) },
                { typeof(IInlineContentItemsResolver<UnretrievedContentItem>), typeof(ReplaceWithWarningAboutUnretrievedItemResolver) },
                { typeof(IInlineContentItemsResolver<UnknownContentItem>), typeof(ReplaceWithWarningAboutUnknownItemResolver) },
                { typeof(IModelProvider), typeof(ModelProvider) },
                { typeof(IPropertyMapper), typeof(PropertyMapper) },
                { typeof(IRetryPolicyProvider), typeof(DefaultRetryPolicyProvider) },
                { typeof(IDeliveryClient), typeof(DeliveryClient) },
            }
        );

        public static IEnumerable<object[]> DeliveryOptionsConfigurationParameters =>
           new[]
           {
                new[] {"as_root"},
                new[] {"under_default_key", "DeliveryOptions"},
                new[] {"under_custom_key", "CustomNameForDeliveryOptions"},
                new[] {"nested_under_default_key", "Options:DeliveryOptions"}
           };


        public ServiceCollectionsExtensionsTests()
        {
            _serviceCollection = new ServiceCollection();
        }

        [Fact]
        public void AddDeliveryClientWithNullDeliveryOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddDeliveryClient(deliveryOptions: null));
        }

        [Fact]
        public void AddDeliveryClientWithNullBuildDeliveryOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddDeliveryClient(buildDeliveryOptions: null));
        }

        [Fact]
        public void AddDeliveryClientWithOptions_AllServicesAreRegistered()
        {
            _serviceCollection.AddDeliveryClient(new DeliveryOptions { ProjectId = ProjectId });
            var provider = _serviceCollection.BuildServiceProvider();
            AssertDefaultServiceCollection(provider, _expectedInterfacesWithImplementationTypes);
        }

        [Theory]
        [MemberData(nameof(DeliveryOptionsConfigurationParameters))]
        public void AddDeliveryClientWithConfiguration_AllServicesAreRegistered(string fileNamePostfix, string customSectionName = null)
        {
            var jsonConfigurationPath = Path.Combine(
                Environment.CurrentDirectory,
                "Fixtures",
                "ServiceCollectionsExtensions",
                $"deliveryOptions_{fileNamePostfix}.json");
            var fakeConfiguration = new ConfigurationBuilder()
                .AddJsonFile(jsonConfigurationPath)
                .Build();

            _serviceCollection.AddDeliveryClient(fakeConfiguration, customSectionName);
            var provider = _serviceCollection.BuildServiceProvider();

            AssertDefaultServiceCollection(provider, _expectedInterfacesWithImplementationTypes);
        }

        private void AssertDefaultServiceCollection(ServiceProvider provider, IDictionary<Type, Type> expectedTypes)
        {
            foreach (var type in expectedTypes)
            {
                var imp = provider.GetRequiredService(type.Key);
                Assert.IsType(type.Value, imp);
            }
        }
    }
}
