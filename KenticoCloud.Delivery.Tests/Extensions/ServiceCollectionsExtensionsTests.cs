using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using KenticoKontent.Delivery.ContentLinks;
using KenticoKontent.Delivery.InlineContentItems;
using KenticoKontent.Delivery.ResiliencePolicy;
using KenticoKontent.Delivery.StrongTyping;
using KenticoKontent.Delivery.Tests.Factories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace KenticoKontent.Delivery.Tests.Extensions
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

        private readonly Type[] _expectedResolvableContentTypes = {
                typeof(object),
                typeof(UnretrievedContentItem),
                typeof(UnknownContentItem)
        };

        private readonly ReadOnlyDictionary<Type, Type> _expectedInterfacesWithImplementationTypes = new ReadOnlyDictionary<Type, Type>(
            new Dictionary<Type, Type>
            {
                { typeof(IOptions<DeliveryOptions>), typeof(IOptions<DeliveryOptions>) },
                { typeof(IContentLinkUrlResolver), typeof(DefaultContentLinkUrlResolver) },
                { typeof(ITypeProvider), typeof(TypeProvider) },
                { typeof(HttpClient), typeof(HttpClient) },
                { typeof(IInlineContentItemsProcessor), typeof(InlineContentItemsProcessor) },
                { typeof(IInlineContentItemsResolver<object>), typeof(ReplaceWithWarningAboutRegistrationResolver) },
                { typeof(IInlineContentItemsResolver<UnretrievedContentItem>), typeof(ReplaceWithWarningAboutUnretrievedItemResolver) },
                { typeof(IInlineContentItemsResolver<UnknownContentItem>), typeof(ReplaceWithWarningAboutUnknownItemResolver) },
                { typeof(IModelProvider), typeof(ModelProvider) },
                { typeof(IPropertyMapper), typeof(PropertyMapper) },
                { typeof(IResiliencePolicyProvider), typeof(DefaultResiliencePolicyProvider) },
                { typeof(IDeliveryClient), typeof(DeliveryClient) }
            }
        );

        private readonly FakeServiceCollection _fakeServiceCollection;

        public ServiceCollectionsExtensionsTests()
        {
            _fakeServiceCollection = new FakeServiceCollection();
        }

        [Fact]
        public void AddDeliveryClientWithDeliveryOptions_AllServicesAreRegistered()
        {
            _fakeServiceCollection.AddDeliveryClient(new DeliveryOptions {ProjectId = ProjectId});

            AssertDefaultServiceCollection(_expectedInterfacesWithImplementationTypes, _expectedResolvableContentTypes);
        }

        [Fact]
        public void AddDeliveryClientWithProjectId_AllServicesAreRegistered()
        {
            _fakeServiceCollection.AddDeliveryClient(builder =>
                builder.WithProjectId(ProjectId).UseProductionApi.Build());

            AssertDefaultServiceCollection(_expectedInterfacesWithImplementationTypes, _expectedResolvableContentTypes);
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
            var expect = GetModifiedExpectedOptionsTypes();

            var jsonConfigurationPath = Path.Combine(
                Environment.CurrentDirectory,
                "Fixtures",
                "ServiceCollectionsExtensions",
                $"deliveryOptions_{fileNamePostfix}.json");
            var fakeConfiguration = new ConfigurationBuilder()
                .AddJsonFile(jsonConfigurationPath)
                .Build();

            _fakeServiceCollection.AddDeliveryClient(fakeConfiguration, customSectionName);

            AssertDefaultServiceCollection(expect, _expectedResolvableContentTypes);
        }

        [Fact]
        public void AddDeliveryInlineContentItemsResolver_RegistersTheResolverImplementation()
        {
            var tweetResolver = InlineContentItemsResolverFactory.Instance.ResolveToMessage<Tweet>("Tweet");
            var hostedVideoResolver = InlineContentItemsResolverFactory.Instance.ResolveToMessage<HostedVideo>("HostedVideo");
            var expectedRegisteredTypes = AddToExpectedInterfacesWithImplementationTypes(
                (typeof(IInlineContentItemsResolver<Tweet>), tweetResolver.GetType()),
                (typeof(IInlineContentItemsResolver<HostedVideo>), hostedVideoResolver.GetType()));
            var expectedResolvableContentTypes = AddToDefaultResolvableContentTypes(
                typeof(Tweet),
                typeof(HostedVideo));

            _fakeServiceCollection
                .AddDeliveryInlineContentItemsResolver(tweetResolver)
                .AddDeliveryClient(new DeliveryOptions { ProjectId = ProjectId })
                .AddDeliveryInlineContentItemsResolver(hostedVideoResolver);

            AssertDefaultServiceCollection(expectedRegisteredTypes, expectedResolvableContentTypes);
        }

        private IDictionary<Type, Type> GetModifiedExpectedOptionsTypes()
        {
            var excludedTypes = new [] { typeof(IOptions<DeliveryOptions>) };

            return _expectedInterfacesWithImplementationTypes
                .Where(pair => !excludedTypes.Contains(pair.Key))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        private IEnumerable<Type> AddToDefaultResolvableContentTypes(params Type[] additionalTypes)
            => _expectedResolvableContentTypes
                .Union(additionalTypes)
                .ToArray();

        private IDictionary<Type, Type> AddToExpectedInterfacesWithImplementationTypes(params (Type contract, Type implementation)[] additionalTypes) => 
            _expectedInterfacesWithImplementationTypes
                .Union(additionalTypes.ToDictionary(
                    addition => addition.contract,
                    addition => addition.implementation))
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value);

        private void AssertDefaultServiceCollection(IDictionary<Type, Type> expectedTypes, IEnumerable<Type> expectedResolvableContentTypes)
        {
            Assert.Equal(ProjectId, _fakeServiceCollection.ProjectId);
            AssertEqualServiceCollectionRegistrations(expectedTypes);
            AssertEqualResolvableContentTypes(expectedResolvableContentTypes);
        }

        private static string GetNameFromRegistration(KeyValuePair<Type, Type> registration) 
            => registration.Value?.FullName ?? registration.Key?.FullName;

        // removes types that are automatically registered by DI framework when DeliveryOptions are registered
        private static bool TypeIsNotOptionsRelated(KeyValuePair<Type, Type> registration)
        {
            var optionsRelatedTypes = new Dictionary<Type, Type>
            {
                {typeof(IConfigureOptions<DeliveryOptions>), null},
                {typeof(IOptionsChangeTokenSource<DeliveryOptions>), null},
                {typeof(IOptions<>), null},
                {typeof(IOptionsSnapshot<>), null},
                {typeof(IOptionsMonitor<>), null},
                {typeof(IOptionsFactory<>), null},
                {typeof(IOptionsMonitorCache<>), null},
            };

            return !optionsRelatedTypes.ContainsKey(registration.Key);
        }

        private void AssertEqualServiceCollectionRegistrations(IDictionary<Type, Type> expectedTypes)
        {
            var missingTypesNames = expectedTypes
                .Except(_fakeServiceCollection.Dependencies)
                .Select(GetNameFromRegistration)
                .ToArray();
            var unexpectedTypesNames = _fakeServiceCollection
                .Dependencies
                .Except(expectedTypes)
                .Where(TypeIsNotOptionsRelated)
                .Select(GetNameFromRegistration)
                .ToArray();

            Assert.Empty(missingTypesNames);
            Assert.Empty(unexpectedTypesNames);
        }

        private void AssertEqualResolvableContentTypes(IEnumerable<Type> expectedResolvableContentTypes)
        {
            var resolvableContentTypes = expectedResolvableContentTypes.ToList();

            var unexpectedResolverNames = _fakeServiceCollection
                .ContentTypesResolvedByResolvers
                .Except(resolvableContentTypes)
                .Select(type => type.FullName);
            var missingResolverNames = resolvableContentTypes
                .Except(_fakeServiceCollection.ContentTypesResolvedByResolvers)
                .Select(type => type.FullName);

            Assert.Empty(unexpectedResolverNames);
            Assert.Empty(missingResolverNames);
        }
    }
}