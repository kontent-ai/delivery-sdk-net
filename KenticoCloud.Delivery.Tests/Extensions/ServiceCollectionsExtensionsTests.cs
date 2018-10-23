using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using KenticoCloud.Delivery.InlineContentItems;
using KenticoCloud.Delivery.ResiliencePolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace KenticoCloud.Delivery.Tests.Extensions
{
    public class ServiceCollectionsExtensionsTests
    {
        public static IEnumerable<object[]> DeliveryOptionsConfigurationParameters =>
            new[]
            {
                new[] {"as_root"},
                new[] {"under_default_key", "DeliveryOptions"},
                new[] {"under_custom_key", "CustomNameForDeliveryOptions"},
                new[] {"nested_under_default_key", "Options:DeliveryOptions"}
            };

        private const string ProjectId = "d79786fb-042c-47ec-8e5c-beaf93e38b84";

        private readonly List<Type> _expectedImplementationTypes = new List<Type>
        {
            typeof(IOptions<DeliveryOptions>),
            typeof(IContentLinkUrlResolver),
            typeof(ICodeFirstTypeProvider),
            typeof(HttpClient),
            typeof(IInlineContentItemsResolver<object>),
            typeof(IInlineContentItemsResolver<UnretrievedContentItem>),
            typeof(IInlineContentItemsProcessor),
            typeof(ICodeFirstModelProvider),
            typeof(ICodeFirstPropertyMapper),
            typeof(IResiliencePolicyProvider),
            typeof(IDeliveryClient)
        };

        private readonly FakeServiceCollection _fakeServiceCollection;

        public ServiceCollectionsExtensionsTests()
        {
            _fakeServiceCollection = new FakeServiceCollection();
        }

        [Fact]
        public void AddDeliveryClientWithDeliveryOptions_AllServicesAreRegistered()
        {
            _fakeServiceCollection.AddDeliveryClient(new DeliveryOptions {ProjectId = ProjectId});

            AssertServiceCollection();
        }

        [Fact]
        public void AddDeliveryClientWithProjectId_AllServicesAreRegistered()
        {
            _fakeServiceCollection.AddDeliveryClient(builder =>
                builder.WithProjectId(ProjectId).UseProductionApi.Build());

            AssertServiceCollection();
        }

        [Fact]
        public void AddDeliveryClientWithNullDeliveryOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _fakeServiceCollection.AddDeliveryClient(deliveryOptions: null));
        }

        [Fact]
        public void AddDeliveryClientWithNullBuildDeliveryOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _fakeServiceCollection.AddDeliveryClient(buildDeliveryOptions: null));
        }

        [Theory]
        [MemberData(nameof(DeliveryOptionsConfigurationParameters))]
        public void AddDeliveryClientWithConfiguration_AllServicesAreRegistered(string fileNamePostfix, string customSectionName = null)
        {
            ModifyExpectedOptionsImplementationTypes();

            var jsonConfigurationPath = Path.Combine(
                Environment.CurrentDirectory,
                "Fixtures",
                "ServiceCollectionsExtensions",
                $"deliveryOptions_{fileNamePostfix}.json");
            var fakeConfiguration = new ConfigurationBuilder().AddJsonFile(jsonConfigurationPath).Build();

            _fakeServiceCollection.AddDeliveryClient(fakeConfiguration, customSectionName);

            AssertServiceCollection();
        }

        private void ModifyExpectedOptionsImplementationTypes()
        {
            _expectedImplementationTypes.Remove(typeof(IOptions<DeliveryOptions>));

            _expectedImplementationTypes.Add(typeof(IConfigureOptions<DeliveryOptions>));
            _expectedImplementationTypes.Add(typeof(IOptionsChangeTokenSource<DeliveryOptions>));
            _expectedImplementationTypes.Add(typeof(IOptions<>));
            _expectedImplementationTypes.Add(typeof(IOptionsSnapshot<>));
            _expectedImplementationTypes.Add(typeof(IOptionsMonitor<>));
            _expectedImplementationTypes.Add(typeof(IOptionsFactory<>));
            _expectedImplementationTypes.Add(typeof(IOptionsMonitorCache<>));
        }

        private void AssertServiceCollection()
        {
            var missingTypesNames = _expectedImplementationTypes
                .Except(_fakeServiceCollection.Dependencies.Keys)
                .Select(type => type.FullName)
                .ToArray();
            var unexpectedTypesNames = _fakeServiceCollection
                .Dependencies
                .Keys
                .Except(_expectedImplementationTypes)
                .Select(type => type.FullName)
                .ToArray();

            Assert.Empty(missingTypesNames);
            Assert.Empty(unexpectedTypesNames);
            Assert.Equal(ProjectId, _fakeServiceCollection.ProjectId);
        }
    }
}