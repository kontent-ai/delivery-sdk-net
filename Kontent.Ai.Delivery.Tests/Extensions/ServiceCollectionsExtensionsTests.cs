using System.Collections.ObjectModel;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Api;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.ContentItems;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Extensions;

public class ServiceCollectionsExtensionsTests
{
    private readonly ServiceCollection _serviceCollection;
    private const string EnvironmentId = "d79786fb-042c-47ec-8e5c-beaf93e38b84";
    private const string PreviewApiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiIwNDllNjllNDBjMDI0NzU3YmE2Y2RmODQzMjI0NGI2ZCIsImlhdCI6MTYyNzkwNDk0MSwibmJmIjoxNjI3OTA0OTQxLCJleHAiOjE5NzM1MDQ5NDEsInZlciI6IjEuMC4wIiwicHJvamVjdF9pZCI6IjZlYTk4NWNlMWM4ZjAwZWRiZmJkNGU1OGJkNGYzZDFiIiwiYXVkIjoicHJldmlldy5kZWxpdmVyLmtlbnRpY29jbG91ZC5jb20ifQ.j5uH5LVIT45bP4VeSkNRynzyR_vqHfHelNXpy7R8C0w";

    private readonly ReadOnlyDictionary<Type, Type> _expectedInterfacesWithImplementationTypes = new(
        new Dictionary<Type, Type>
        {
            { typeof(ITypeProvider), typeof(TypeProvider) },
            { typeof(IItemTypingStrategy), typeof(DefaultItemTypingStrategy) },
            { typeof(IContentDeserializer), typeof(ContentDeserializer) },
            { typeof(IHtmlParser), typeof(HtmlParser) },
            { typeof(IPropertyMapper), typeof(PropertyMapper) },
            { typeof(IDeliveryClient), typeof(DeliveryClient) },
        }
    );

    public static IEnumerable<object[]> DeliveryOptionsConfigurationParameters =>
       [
            new[] {"as_root"},
            ["under_default_key", "DeliveryOptions"],
            ["under_custom_key", "CustomNameForDeliveryOptions"],
            ["nested_under_default_key", "Options:DeliveryOptions"]
       ];


    public ServiceCollectionsExtensionsTests()
    {
        _serviceCollection = new ServiceCollection();
    }

    [Fact]
    public void AddDeliveryClientWithNullDeliveryOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddDeliveryClient(deliveryOptions: null!));
    }

    [Fact]
    public void AddDeliveryClientWithNullBuildDeliveryOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddDeliveryClient(configureOptions: null!));
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
            (IDeliveryOptionsBuilder b) => b.WithEnvironmentId(EnvironmentId).UsePreviewApi(PreviewApiKey).Build());

        var provider = _serviceCollection.BuildServiceProvider();
        AssertDefaultServiceCollection(provider, _expectedInterfacesWithImplementationTypes);

        var monitor = provider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
        var options = monitor.CurrentValue;
        Assert.Equal(EnvironmentId, options.EnvironmentId);
        Assert.True(options.UsePreviewApi);
        Assert.Equal(PreviewApiKey, options.PreviewApiKey);
    }

    [Fact]
    public void AddDeliveryClientWithBuilderDelegate_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddDeliveryClient(
            buildDeliveryOptions: null!));
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
        var provider = _serviceCollection.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
        Assert.Throws<OptionsValidationException>(() => optionsMonitor.CurrentValue);
    }

    [Fact]
    public void AddDeliveryClient_PreviewMissingApiKey_ThrowsOptionsValidationException()
    {
        _serviceCollection.AddDeliveryClient(new DeliveryOptions { EnvironmentId = EnvironmentId, UsePreviewApi = true, PreviewApiKey = null });
        var provider = _serviceCollection.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
        Assert.Throws<OptionsValidationException>(() => optionsMonitor.CurrentValue);
    }

    [Fact]
    public void AddDeliveryClient_SecureAccessMissingApiKey_ThrowsOptionsValidationException()
    {
        _serviceCollection.AddDeliveryClient(new DeliveryOptions { EnvironmentId = EnvironmentId, UseSecureAccess = true, SecureAccessApiKey = null });
        var provider = _serviceCollection.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
        Assert.Throws<OptionsValidationException>(() => optionsMonitor.CurrentValue);
    }

    [Fact]
    public void AddDeliveryClient_PreviewAndSecureBothTrue_ThrowsOptionsValidationException()
    {
        _serviceCollection.AddDeliveryClient(new DeliveryOptions
        {
            EnvironmentId = EnvironmentId,
            UsePreviewApi = true,
            PreviewApiKey = PreviewApiKey,
            UseSecureAccess = true,
            SecureAccessApiKey = PreviewApiKey
        });

        var provider = _serviceCollection.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
        Assert.Throws<OptionsValidationException>(() => optionsMonitor.CurrentValue);
    }

    [Theory]
    [MemberData(nameof(DeliveryOptionsConfigurationParameters))]
    public void AddDeliveryClientWithConfiguration_AllServicesAreRegistered(string fileNamePostfix, string? customSectionName = null)
    {
        var jsonConfigurationPath = Path.Combine(
            Environment.CurrentDirectory,
            "Fixtures",
            "ServiceCollectionsExtensions",
            $"deliveryOptions_{fileNamePostfix}.json");
        var fakeConfiguration = new ConfigurationBuilder()
            .AddJsonFile(jsonConfigurationPath)
            .Build();

        _serviceCollection.AddDeliveryClient(fakeConfiguration, customSectionName!);
        var provider = _serviceCollection.BuildServiceProvider();

        AssertDefaultServiceCollection(provider, _expectedInterfacesWithImplementationTypes);
    }

    [Fact]
    public void AddDeliveryClient_WithNullConfigurationSection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _serviceCollection.AddDeliveryClient(configurationSection: null!));
    }

    [Fact]
    public void AddDeliveryClient_WithConfigurationSection_AllServicesAreRegistered()
    {
        // Arrange
        var jsonConfigurationPath = Path.Combine(
            Environment.CurrentDirectory,
            "Fixtures",
            "ServiceCollectionsExtensions",
            "deliveryOptions_under_default_key.json");
        var fakeConfiguration = new ConfigurationBuilder()
            .AddJsonFile(jsonConfigurationPath)
            .Build();

        var section = fakeConfiguration.GetSection("DeliveryOptions");

        // Act
        _serviceCollection.AddDeliveryClient(section);
        var provider = _serviceCollection.BuildServiceProvider();

        // Assert
        AssertDefaultServiceCollection(provider, _expectedInterfacesWithImplementationTypes);

        // Verify options were correctly bound from configuration section
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
        Assert.Equal(EnvironmentId, optionsMonitor.CurrentValue.EnvironmentId);
    }

    private void AssertDefaultServiceCollection(ServiceProvider provider, IDictionary<Type, Type> expectedTypes)
    {
        foreach (var type in expectedTypes)
        {
            var imp = provider.GetRequiredService(type.Key);
            Assert.IsType(type.Value, imp);
        }
    }

    #region Integration Tests for Multi-Client and Runtime Configuration

    [Fact]
    public void AddDeliveryClient_MultipleNamedClients_AllRegisteredWithSeparateOptions()
    {
        // Arrange
        const string envId1 = "11111111-1111-1111-1111-111111111111";
        const string envId2 = "22222222-2222-2222-2222-222222222222";

        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = envId1;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDeliveryClient("preview", o =>
        {
            o.EnvironmentId = envId2;
            o.UsePreviewApi = true;
            o.PreviewApiKey = PreviewApiKey;
            o.EnableResilience = false;
        });

        // Act
        var provider = _serviceCollection.BuildServiceProvider();

        // Verify factory is registered
        var factory = provider.GetRequiredService<IDeliveryClientFactory>();
        Assert.NotNull(factory);

        // Verify each client has its own options
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
        var prodOptions = optionsMonitor.Get("production");
        var previewOptions = optionsMonitor.Get("preview");

        // Assert - Options should be separate and correctly configured
        Assert.Equal(envId1, prodOptions.EnvironmentId);
        Assert.Equal(envId2, previewOptions.EnvironmentId);
        Assert.False(prodOptions.UsePreviewApi);
        Assert.True(previewOptions.UsePreviewApi);
        Assert.Equal(PreviewApiKey, previewOptions.PreviewApiKey);
    }

    [Fact]
    public void AddDeliveryClient_WithCustomBaseUrl_UsesCustomEndpoint()
    {
        // Arrange
        const string customBase = "https://custom-delivery.example.com";
        _serviceCollection.AddDeliveryClient("custom", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.ProductionEndpoint = customBase;
            o.EnableResilience = false;
        });

        // Act
        var provider = _serviceCollection.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
        var options = optionsMonitor.Get("custom");

        // Assert
        Assert.Equal(customBase, options.GetBaseUrl());
    }

    [Fact]
    public void AddDeliveryClient_RuntimeConfigurationChanges_ReflectedInOptions()
    {
        // Arrange - Start with production API
        var initialOptions = new DeliveryOptions
        {
            EnvironmentId = EnvironmentId,
            UsePreviewApi = false
        };

        _serviceCollection.Configure<DeliveryOptions>("dynamic", opts =>
        {
            opts.EnvironmentId = initialOptions.EnvironmentId;
            opts.UsePreviewApi = initialOptions.UsePreviewApi;
        });

        var provider = _serviceCollection.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();

        // Act 1 - Verify initial state
        var options1 = optionsMonitor.Get("dynamic");
        Assert.False(options1.UsePreviewApi);

        // Act 2 - Simulate runtime configuration change to preview API
        _serviceCollection.Configure<DeliveryOptions>("dynamic", opts =>
        {
            opts.EnvironmentId = EnvironmentId;
            opts.UsePreviewApi = true;
            opts.PreviewApiKey = PreviewApiKey;
        });

        // Rebuild provider to apply changes (in real apps this happens via IOptionsMonitor callbacks)
        var provider2 = _serviceCollection.BuildServiceProvider();
        var optionsMonitor2 = provider2.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
        var options2 = optionsMonitor2.Get("dynamic");

        // Assert - Options reflect the runtime change
        Assert.True(options2.UsePreviewApi);
        Assert.Equal(PreviewApiKey, options2.PreviewApiKey);
    }

    [Fact]
    public void AddDeliveryClient_DuplicateClientName_ThrowsInvalidOperationException()
    {
        // Arrange
        _serviceCollection.AddDeliveryClient("duplicate", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _serviceCollection.AddDeliveryClient("duplicate", o => o.EnvironmentId = EnvironmentId));

        Assert.Contains("duplicate", exception.Message);
        Assert.Contains("already been registered", exception.Message);
        Assert.Contains("Kontent.Ai.Delivery.HttpClient.duplicate", exception.Message);
    }

    [Fact]
    public void AddDeliveryClient_WithNameContainingSpaces_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _serviceCollection.AddDeliveryClient("name with spaces", o => o.EnvironmentId = EnvironmentId));

        Assert.Contains("cannot be empty, contain leading/trailing whitespace, or contain spaces", exception.Message);
        Assert.Contains("Use underscores or hyphens instead", exception.Message);
    }

    [Fact]
    public void AddDeliveryClient_WithNameWithLeadingWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _serviceCollection.AddDeliveryClient(" leading-space", o => o.EnvironmentId = EnvironmentId));

        Assert.Contains("cannot be empty, contain leading/trailing whitespace, or contain spaces", exception.Message);
    }

    [Fact]
    public void AddDeliveryClient_DefaultClient_AccessibleViaFactoryAndDirectInjection()
    {
        // Arrange
        _serviceCollection.AddDeliveryClient(o => o.EnvironmentId = EnvironmentId);

        // Act
        var provider = _serviceCollection.BuildServiceProvider();
        var clientDirect = provider.GetRequiredService<IDeliveryClient>();
        var factory = provider.GetRequiredService<IDeliveryClientFactory>();
        var clientFromFactory = factory.Get(Abstractions.Options.DefaultName);

        // Assert - Should be the same singleton instance
        Assert.Same(clientDirect, clientFromFactory);
    }

    #endregion

    #region Per-Client Caching Tests

    [Fact]
    public void AddDeliveryMemoryCache_RegistersKeyedCacheManager()
    {
        // Arrange
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDeliveryMemoryCache("production");

        // Act
        var provider = _serviceCollection.BuildServiceProvider();
        var cacheManager = provider.GetKeyedService<IDeliveryCacheManager>("production");

        // Assert
        Assert.NotNull(cacheManager);
        Assert.IsType<MemoryCacheManager>(cacheManager);
    }

    [Fact]
    public void AddDeliveryDistributedCache_RegistersKeyedCacheManager()
    {
        // Arrange
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDistributedMemoryCache(); // Register IDistributedCache
        _serviceCollection.AddDeliveryDistributedCache("production");

        // Act
        var provider = _serviceCollection.BuildServiceProvider();
        var cacheManager = provider.GetKeyedService<IDeliveryCacheManager>("production");

        // Assert
        Assert.NotNull(cacheManager);
        Assert.IsType<DistributedCacheManager>(cacheManager);
    }

    [Fact]
    public void AddDeliveryMemoryCache_MultipleClients_RegistersSeparateKeyedManagers()
    {
        // Arrange
        const string envId1 = "11111111-1111-1111-1111-111111111111";
        const string envId2 = "22222222-2222-2222-2222-222222222222";

        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = envId1;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDeliveryClient("preview", o =>
        {
            o.EnvironmentId = envId2;
            o.UsePreviewApi = true;
            o.PreviewApiKey = PreviewApiKey;
            o.EnableResilience = false;
        });

        _serviceCollection.AddDeliveryMemoryCache("production", keyPrefix: "prod");
        _serviceCollection.AddDeliveryMemoryCache("preview", keyPrefix: "preview");

        // Act
        var provider = _serviceCollection.BuildServiceProvider();
        var prodCacheManager = provider.GetKeyedService<IDeliveryCacheManager>("production");
        var previewCacheManager = provider.GetKeyedService<IDeliveryCacheManager>("preview");

        // Assert - Both should be registered and be different instances
        Assert.NotNull(prodCacheManager);
        Assert.NotNull(previewCacheManager);
        Assert.NotSame(prodCacheManager, previewCacheManager);
    }

    [Fact]
    public void AddDeliveryMemoryCache_ClientWithoutCacheRegistration_ReturnsNull()
    {
        // Arrange - Register client but NOT cache
        _serviceCollection.AddDeliveryClient("no-cache", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });

        // Act
        var provider = _serviceCollection.BuildServiceProvider();
        var cacheManager = provider.GetKeyedService<IDeliveryCacheManager>("no-cache");

        // Assert - No cache manager should be registered for this client
        Assert.Null(cacheManager);
    }

    [Fact]
    public void AddDeliveryMemoryCache_WithCustomExpiration_PassesExpirationToManager()
    {
        // Arrange
        var expiration = TimeSpan.FromMinutes(30);
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDeliveryMemoryCache("production", defaultExpiration: expiration);

        // Act
        var provider = _serviceCollection.BuildServiceProvider();
        var cacheManager = provider.GetKeyedService<IDeliveryCacheManager>("production");

        // Assert
        Assert.NotNull(cacheManager);
        Assert.IsType<MemoryCacheManager>(cacheManager);
    }

    [Fact]
    public void AddDeliveryMemoryCache_NullClientName_ThrowsArgumentNullException()
    {
        // Act & Assert - use named parameter to ensure we call the string overload
        Assert.Throws<ArgumentNullException>(() =>
            _serviceCollection.AddDeliveryMemoryCache(clientName: null!));
    }

    [Fact]
    public void AddDeliveryMemoryCache_EmptyClientName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _serviceCollection.AddDeliveryMemoryCache(clientName: ""));
    }

    [Fact]
    public void AddDeliveryDistributedCache_NullClientName_ThrowsArgumentNullException()
    {
        // Act & Assert - use named parameter to ensure we call the string overload
        Assert.Throws<ArgumentNullException>(() =>
            _serviceCollection.AddDeliveryDistributedCache(clientName: null!));
    }

    [Fact]
    public void AddDeliveryMemoryCache_RegistersContentDependencyExtractor_CacheFirst()
    {
        // Arrange - Cache registered before client
        _serviceCollection.AddDeliveryMemoryCache("production");
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });

        // Act
        var provider = _serviceCollection.BuildServiceProvider();
        var extractor = provider.GetService<IContentDependencyExtractor>();

        // Assert - Should register the real extractor (not null extractor)
        Assert.NotNull(extractor);
        Assert.DoesNotContain("Null", extractor.GetType().Name);
    }

    [Fact]
    public void AddDeliveryMemoryCache_RegistersContentDependencyExtractor_ClientFirst()
    {
        // Arrange - Client registered before cache (order should not matter)
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDeliveryMemoryCache("production");

        // Act
        var provider = _serviceCollection.BuildServiceProvider();
        var extractor = provider.GetService<IContentDependencyExtractor>();

        // Assert - Should register the real extractor regardless of order
        Assert.NotNull(extractor);
        Assert.DoesNotContain("Null", extractor.GetType().Name);
    }

    [Fact]
    public void AddDeliveryDistributedCache_RegistersContentDependencyExtractor_CacheFirst()
    {
        // Arrange - Cache registered before client
        _serviceCollection.AddDistributedMemoryCache();
        _serviceCollection.AddDeliveryDistributedCache("production");
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });

        // Act
        var provider = _serviceCollection.BuildServiceProvider();
        var extractor = provider.GetService<IContentDependencyExtractor>();

        // Assert - Should register the real extractor (not null extractor)
        Assert.NotNull(extractor);
        Assert.DoesNotContain("Null", extractor.GetType().Name);
    }

    [Fact]
    public void AddDeliveryDistributedCache_RegistersContentDependencyExtractor_ClientFirst()
    {
        // Arrange - Client registered before cache (order should not matter)
        _serviceCollection.AddDistributedMemoryCache();
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDeliveryDistributedCache("production");

        // Act
        var provider = _serviceCollection.BuildServiceProvider();
        var extractor = provider.GetService<IContentDependencyExtractor>();

        // Assert - Should register the real extractor regardless of order
        Assert.NotNull(extractor);
        Assert.DoesNotContain("Null", extractor.GetType().Name);
    }

    #endregion

    #region Keyed Service Fallback Tests

    [Fact]
    public void MultipleNamedClients_OneWithCache_OtherWithoutCache()
    {
        // Arrange
        const string envId1 = "11111111-1111-1111-1111-111111111111";
        const string envId2 = "22222222-2222-2222-2222-222222222222";

        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = envId1;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDeliveryClient("preview", o =>
        {
            o.EnvironmentId = envId2;
            o.UsePreviewApi = true;
            o.PreviewApiKey = PreviewApiKey;
            o.EnableResilience = false;
        });

        // Only enable caching for production
        _serviceCollection.AddDeliveryMemoryCache("production", keyPrefix: "prod");

        // Act
        var provider = _serviceCollection.BuildServiceProvider();
        var prodCacheManager = provider.GetKeyedService<IDeliveryCacheManager>("production");
        var previewCacheManager = provider.GetKeyedService<IDeliveryCacheManager>("preview");

        // Assert - Only production has cache
        Assert.NotNull(prodCacheManager);
        Assert.Null(previewCacheManager);
    }

    [Fact]
    public void AddDeliveryMemoryCache_SharedUnderlyingMemoryCache()
    {
        // Arrange - Both clients use the same IMemoryCache but different key prefixes
        _serviceCollection.AddDeliveryClient("client1", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDeliveryClient("client2", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });

        _serviceCollection.AddDeliveryMemoryCache("client1", keyPrefix: "prefix1");
        _serviceCollection.AddDeliveryMemoryCache("client2", keyPrefix: "prefix2");

        // Act
        var provider = _serviceCollection.BuildServiceProvider();

        // Both cache managers should resolve to separate keyed instances
        var cache1 = provider.GetKeyedService<IDeliveryCacheManager>("client1");
        var cache2 = provider.GetKeyedService<IDeliveryCacheManager>("client2");

        // But they share the underlying IMemoryCache (singleton)
        var memoryCache = provider.GetService<IMemoryCache>();

        // Assert
        Assert.NotNull(cache1);
        Assert.NotNull(cache2);
        Assert.NotSame(cache1, cache2);
        Assert.NotNull(memoryCache);
    }

    #endregion
}
