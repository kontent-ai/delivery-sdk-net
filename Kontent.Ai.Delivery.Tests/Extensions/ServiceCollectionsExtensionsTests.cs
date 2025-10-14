using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Api;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Extensions;

public class ServiceCollectionsExtensionsTests
{
    private readonly ServiceCollection _serviceCollection;
    private const string EnvironmentId = "d79786fb-042c-47ec-8e5c-beaf93e38b84";

    private readonly ReadOnlyDictionary<Type, Type> _expectedInterfacesWithImplementationTypes = new ReadOnlyDictionary<Type, Type>(
        new Dictionary<Type, Type>
        {
            { typeof(ITypeProvider), typeof(TypeProvider) },
            { typeof(IItemTypingStrategy), typeof(DefaultItemTypingStrategy) },
            { typeof(IContentDeserializer), typeof(ContentDeserializer) },
            { typeof(IElementsPostProcessor), typeof(ElementsPostProcessor) },
            { typeof(IHtmlParser), typeof(HtmlParser) },
            { typeof(IPropertyMapper), typeof(PropertyMapper) },
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
        Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddDeliveryClient(configureOptions: null));
    }

    [Fact]
    public void AddDeliveryClientWithOptions_AllServicesAreRegistered()
    {
        _serviceCollection.AddDeliveryClient(new DeliveryOptions { EnvironmentId = EnvironmentId });
        var provider = _serviceCollection.BuildServiceProvider();
        AssertDefaultServiceCollection(provider, _expectedInterfacesWithImplementationTypes);
    }

    [Fact]
    public void AddDeliveryClientWithConfigureAction_AllServicesAreRegistered()
    {
        _serviceCollection.AddDeliveryClient(o => o.EnvironmentId = EnvironmentId);
        var provider = _serviceCollection.BuildServiceProvider();
        AssertDefaultServiceCollection(provider, _expectedInterfacesWithImplementationTypes);
    }

    [Fact]
    public void AddDeliveryClientWithBuilderDelegate_AllServicesAreRegistered_AndOptionsApplied()
    {
        _serviceCollection.AddDeliveryClient(
            (IDeliveryOptionsBuilder b) => b.WithEnvironmentId(EnvironmentId).UsePreviewApi("preview_key").Build());

        var provider = _serviceCollection.BuildServiceProvider();
        AssertDefaultServiceCollection(provider, _expectedInterfacesWithImplementationTypes);

        var monitor = provider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
        var options = monitor.CurrentValue;
        Assert.Equal(EnvironmentId, options.EnvironmentId);
        Assert.True(options.UsePreviewApi);
        Assert.Equal("preview_key", options.PreviewApiKey);
    }

    [Fact]
    public void AddDeliveryClientWithBuilderDelegate_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddDeliveryClient(
            buildDeliveryOptions: null));
    }

    [Fact]
    public void AddDeliveryClient_Advanced_InvokesConfigureRefit()
    {
        bool invoked = false;
        _serviceCollection.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = EnvironmentId },
            configureHttpClient: null,
            configureResilience: null,
            configureRefit: s => invoked = true);

        // Build triggers ValidateOnStart but not needed for this assertion
        _serviceCollection.BuildServiceProvider();
        Assert.True(invoked);
    }

    [Fact]
    public void AddDeliveryClient_ResilienceEnabled_InvokesConfigureResilience()
    {
        bool invoked = false;
        _serviceCollection.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = EnvironmentId, EnableResilience = true },
            configureHttpClient: null,
            configureResilience: _ => invoked = true,
            configureRefit: null);

        var provider = _serviceCollection.BuildServiceProvider();
        // Resolve the typed client to force HttpClient pipeline building
        var _ = provider.GetRequiredService<IDeliveryApi>();
        Assert.True(invoked);
    }

    [Fact]
    public void AddDeliveryClient_ResilienceDisabled_DoesNotInvokeConfigureResilience()
    {
        bool invoked = false;
        _serviceCollection.AddDeliveryClient(
            new DeliveryOptions { EnvironmentId = EnvironmentId, EnableResilience = false },
            configureHttpClient: null,
            configureResilience: _ => invoked = true,
            configureRefit: null);

        var provider = _serviceCollection.BuildServiceProvider();
        var _ = provider.GetRequiredService<IDeliveryApi>();
        Assert.False(invoked);
    }

    [Fact]
    public void AddDeliveryClient_InvalidEnvironmentId_ThrowsOptionsValidationException()
    {
        _serviceCollection.AddDeliveryClient(new DeliveryOptions { EnvironmentId = "not-a-guid" });
        Assert.Throws<OptionsValidationException>(() => _serviceCollection.BuildServiceProvider());
    }

    [Fact]
    public void AddDeliveryClient_PreviewMissingApiKey_ThrowsOptionsValidationException()
    {
        _serviceCollection.AddDeliveryClient(new DeliveryOptions { EnvironmentId = EnvironmentId, UsePreviewApi = true, PreviewApiKey = null });
        Assert.Throws<OptionsValidationException>(() => _serviceCollection.BuildServiceProvider());
    }

    [Fact]
    public void AddDeliveryClient_SecureAccessMissingApiKey_ThrowsOptionsValidationException()
    {
        _serviceCollection.AddDeliveryClient(new DeliveryOptions { EnvironmentId = EnvironmentId, UseSecureAccess = true, SecureAccessApiKey = null });
        Assert.Throws<OptionsValidationException>(() => _serviceCollection.BuildServiceProvider());
    }

    [Fact]
    public void AddDeliveryClient_PreviewAndSecureBothTrue_ThrowsOptionsValidationException()
    {
        _serviceCollection.AddDeliveryClient(new DeliveryOptions
        {
            EnvironmentId = EnvironmentId,
            UsePreviewApi = true,
            PreviewApiKey = "preview",
            UseSecureAccess = true,
            SecureAccessApiKey = "secure"
        });

        Assert.Throws<OptionsValidationException>(() => _serviceCollection.BuildServiceProvider());
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
