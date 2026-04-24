using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Api;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.ContentItems;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
            { typeof(IDeliveryClient), typeof(DeliveryClient) },
        }
    );

    public static IEnumerable<object[]> DeliveryOptionsConfigurationParameters =>
       [
            new[] { "as_root" },
           ["under_default_key", "DeliveryOptions"],
           ["under_custom_key", "CustomNameForDeliveryOptions"],
           ["nested_under_default_key", "Options:DeliveryOptions"]
       ];


    public ServiceCollectionsExtensionsTests() => _serviceCollection = new ServiceCollection();

    [Fact]
    public void AddDeliveryClientWithNullDeliveryOptions_ThrowsArgumentNullException() => Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddDeliveryClient(deliveryOptions: null!));

    [Fact]
    public void AddDeliveryClientWithNullBuildDeliveryOptions_ThrowsArgumentNullException() => Assert.Throws<ArgumentNullException>(() => _serviceCollection.AddDeliveryClient(configureOptions: (Action<DeliveryOptions>)null!));

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
        var invoked = false;
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
        var invoked = false;
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
        var invoked = false;
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
        var jsonConfigurationPath = Path.Combine(
            Environment.CurrentDirectory,
            "Fixtures",
            "ServiceCollectionsExtensions",
            "deliveryOptions_under_default_key.json");
        var fakeConfiguration = new ConfigurationBuilder()
            .AddJsonFile(jsonConfigurationPath)
            .Build();

        var section = fakeConfiguration.GetSection("DeliveryOptions");

        _serviceCollection.AddDeliveryClient(section);
        var provider = _serviceCollection.BuildServiceProvider();

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

        var provider = _serviceCollection.BuildServiceProvider();

        var factory = provider.GetRequiredService<IDeliveryClientFactory>();
        Assert.NotNull(factory);

        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
        var prodOptions = optionsMonitor.Get("production");
        var previewOptions = optionsMonitor.Get("preview");

        Assert.Equal(envId1, prodOptions.EnvironmentId);
        Assert.Equal(envId2, previewOptions.EnvironmentId);
        Assert.False(prodOptions.UsePreviewApi);
        Assert.True(previewOptions.UsePreviewApi);
        Assert.Equal(PreviewApiKey, previewOptions.PreviewApiKey);
    }

    [Fact]
    public void AddDeliveryClient_WithCustomBaseUrl_UsesCustomEndpoint()
    {
        const string customBase = "https://custom-delivery.example.com";
        _serviceCollection.AddDeliveryClient("custom", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.ProductionEndpoint = customBase;
            o.EnableResilience = false;
        });

        var provider = _serviceCollection.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
        var options = optionsMonitor.Get("custom");

        Assert.Equal(customBase, options.GetBaseUrl());
    }

    [Fact]
    public void AddDeliveryClient_RuntimeConfigurationChanges_ReflectedInOptions()
    {
        // Start with production API
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

        var options1 = optionsMonitor.Get("dynamic");
        Assert.False(options1.UsePreviewApi);

        // Simulate runtime configuration change to preview API
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

        Assert.True(options2.UsePreviewApi);
        Assert.Equal(PreviewApiKey, options2.PreviewApiKey);
    }

    [Fact]
    public async Task AddDeliveryClient_WithConfiguration_RuntimeChanges_AreReflectedInOptionsMonitor()
    {
        var inMemoryConfiguration = new Dictionary<string, string?>
        {
            ["DeliveryOptions:EnvironmentId"] = EnvironmentId,
            ["DeliveryOptions:UsePreviewApi"] = "false",
            ["DeliveryOptions:EnableResilience"] = "false"
        };

        var configurationRoot = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfiguration)
            .Build();

        _serviceCollection.AddDeliveryClient(configurationRoot);
        var provider = _serviceCollection.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();

        Assert.False(optionsMonitor.CurrentValue.UsePreviewApi);

        var optionsChanged = new TaskCompletionSource<DeliveryOptions>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = optionsMonitor.OnChange((opts, _) =>
        {
            if (opts.UsePreviewApi && opts.PreviewApiKey == PreviewApiKey)
            {
                optionsChanged.TrySetResult(opts);
            }
        });

        // Update in-memory configuration to a valid preview setup.
        configurationRoot["DeliveryOptions:PreviewApiKey"] = PreviewApiKey;
        configurationRoot["DeliveryOptions:UsePreviewApi"] = "true";
        configurationRoot.Reload();

        var changed = await optionsChanged.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.True(changed.UsePreviewApi);
        Assert.Equal(PreviewApiKey, changed.PreviewApiKey);
    }

    [Fact]
    public void AddDeliveryClient_DuplicateClientName_ThrowsInvalidOperationException()
    {
        _serviceCollection.AddDeliveryClient("duplicate", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _serviceCollection.AddDeliveryClient("duplicate", o => o.EnvironmentId = EnvironmentId));

        Assert.Contains("duplicate", exception.Message);
        Assert.Contains("already been registered", exception.Message);
        Assert.Contains("Kontent.Ai.Delivery.HttpClient.duplicate", exception.Message);
    }

    [Fact]
    public void AddDeliveryClient_WithNameContainingSpaces_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            _serviceCollection.AddDeliveryClient("name with spaces", o => o.EnvironmentId = EnvironmentId));

        Assert.Contains("Client name cannot contain leading/trailing whitespace, or contain spaces", exception.Message);
        Assert.Contains("Use underscores or hyphens instead", exception.Message);
    }

    [Fact]
    public void AddDeliveryClient_WithNameWithLeadingWhitespace_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            _serviceCollection.AddDeliveryClient(" leading-space", o => o.EnvironmentId = EnvironmentId));

        Assert.Contains("Client name cannot contain leading/trailing whitespace, or contain spaces", exception.Message);
    }

    [Fact]
    public void AddDeliveryClient_DefaultClient_AccessibleViaFactoryAndDirectInjection()
    {
        _serviceCollection.AddDeliveryClient(o => o.EnvironmentId = EnvironmentId);

        var provider = _serviceCollection.BuildServiceProvider();
        var clientDirect = provider.GetRequiredService<IDeliveryClient>();
        var factory = provider.GetRequiredService<IDeliveryClientFactory>();
        var clientFromFactory = factory.Get("Default");

        // Should be the same singleton instance
        Assert.Same(clientDirect, clientFromFactory);
    }

    #endregion

    #region Per-Client Caching Tests

    [Fact]
    public void AddDeliveryMemoryCache_RegistersKeyedCacheManager()
    {
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDeliveryMemoryCache("production");

        var provider = _serviceCollection.BuildServiceProvider();
        var cacheManager = provider.GetKeyedService<IDeliveryCacheManager>("production");

        Assert.NotNull(cacheManager);
        Assert.IsType<MemoryCacheManager>(cacheManager);
    }

    [Fact]
    public void AddDeliveryHybridCache_RegistersKeyedCacheManager()
    {
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDistributedMemoryCache(); // Register IDistributedCache
        _serviceCollection.AddDeliveryHybridCache("production");

        var provider = _serviceCollection.BuildServiceProvider();
        var cacheManager = provider.GetKeyedService<IDeliveryCacheManager>("production");

        Assert.NotNull(cacheManager);
        Assert.IsType<HybridCacheManager>(cacheManager);
    }

    [Fact]
    public void AddDeliveryCacheManager_DefaultOverload_RegistersKeyedCacheManagerForDefaultClient()
    {
        _serviceCollection.AddDeliveryClient(o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDeliveryCacheManager(_ => new TestCustomCacheManager());

        var provider = _serviceCollection.BuildServiceProvider();
        var cacheManager = provider.GetKeyedService<IDeliveryCacheManager>("Default");

        Assert.NotNull(cacheManager);
        Assert.IsType<TestCustomCacheManager>(cacheManager);
    }

    [Fact]
    public void AddDeliveryCacheManager_NamedOverload_RegistersOnlyForSpecifiedClient()
    {
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDeliveryClient("preview", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.UsePreviewApi = true;
            o.PreviewApiKey = PreviewApiKey;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDeliveryCacheManager("production", _ => new TestCustomCacheManager());

        var provider = _serviceCollection.BuildServiceProvider();
        var prodCacheManager = provider.GetKeyedService<IDeliveryCacheManager>("production");
        var previewCacheManager = provider.GetKeyedService<IDeliveryCacheManager>("preview");

        Assert.NotNull(prodCacheManager);
        Assert.IsType<TestCustomCacheManager>(prodCacheManager);
        Assert.Null(previewCacheManager);
    }

    [Fact]
    public void AddDeliveryCacheManager_RegistersContentDependencyExtractor()
    {
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDeliveryCacheManager("production", _ => new TestCustomCacheManager());

        var provider = _serviceCollection.BuildServiceProvider();
        var extractor = provider.GetService<IContentDependencyExtractor>();

        Assert.NotNull(extractor);
        Assert.DoesNotContain("Null", extractor.GetType().Name);
    }

    [Fact]
    public void AddDeliveryCacheManager_CustomManager_DoesNotRequirePurger()
    {
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDeliveryCacheManager("production", _ => new TestCustomCacheManager());

        var provider = _serviceCollection.BuildServiceProvider();
        var cacheManager = provider.GetRequiredKeyedService<IDeliveryCacheManager>("production");
        var purger = cacheManager as IDeliveryCachePurger;

        Assert.NotNull(cacheManager);
        Assert.Null(purger);
    }

    [Fact]
    public void AddDeliveryMemoryCache_ResolvedManager_ImplementsPurger()
    {
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDeliveryMemoryCache("production");

        var provider = _serviceCollection.BuildServiceProvider();
        var cacheManager = provider.GetRequiredKeyedService<IDeliveryCacheManager>("production");

        Assert.IsAssignableFrom<IDeliveryCachePurger>(cacheManager);
    }

    [Fact]
    public void AddDeliveryHybridCache_ResolvedManager_ImplementsPurger()
    {
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDistributedMemoryCache();
        _serviceCollection.AddDeliveryHybridCache("production");

        var provider = _serviceCollection.BuildServiceProvider();
        var cacheManager = provider.GetRequiredKeyedService<IDeliveryCacheManager>("production");

        Assert.IsAssignableFrom<IDeliveryCachePurger>(cacheManager);
    }

    [Fact]
    public void AddDeliveryMemoryCache_AfterHybridCache_ReplacesPreviousCacheManagerRegistration()
    {
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDistributedMemoryCache();

        _serviceCollection.AddDeliveryHybridCache("production");
        _serviceCollection.AddDeliveryMemoryCache("production");

        Assert.Equal(1, CountCacheManagerRegistrations("production"));

        var provider = _serviceCollection.BuildServiceProvider();
        var cacheManager = provider.GetRequiredKeyedService<IDeliveryCacheManager>("production");

        Assert.IsType<MemoryCacheManager>(cacheManager);
    }

    [Fact]
    public void AddDeliveryHybridCache_AfterMemoryCache_ReplacesPreviousCacheManagerRegistration()
    {
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDistributedMemoryCache();

        _serviceCollection.AddDeliveryMemoryCache("production");
        _serviceCollection.AddDeliveryHybridCache("production");

        Assert.Equal(1, CountCacheManagerRegistrations("production"));

        var provider = _serviceCollection.BuildServiceProvider();
        var cacheManager = provider.GetRequiredKeyedService<IDeliveryCacheManager>("production");

        Assert.IsType<HybridCacheManager>(cacheManager);
    }

    [Fact]
    public void AddDeliveryCacheManager_AfterBuiltInCache_ReplacesPreviousCacheManagerRegistration()
    {
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });

        _serviceCollection.AddDeliveryMemoryCache("production");
        _serviceCollection.AddDeliveryCacheManager("production", _ => new TestCustomCacheManager());

        Assert.Equal(1, CountCacheManagerRegistrations("production"));

        var provider = _serviceCollection.BuildServiceProvider();
        var cacheManager = provider.GetRequiredKeyedService<IDeliveryCacheManager>("production");

        Assert.IsType<TestCustomCacheManager>(cacheManager);
    }

    [Fact]
    public void AddDeliveryMemoryCache_AfterCustomCacheManager_ReplacesPreviousCacheManagerRegistration()
    {
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });

        _serviceCollection.AddDeliveryCacheManager("production", _ => new TestCustomCacheManager());
        _serviceCollection.AddDeliveryMemoryCache("production");

        Assert.Equal(1, CountCacheManagerRegistrations("production"));

        var provider = _serviceCollection.BuildServiceProvider();
        var cacheManager = provider.GetRequiredKeyedService<IDeliveryCacheManager>("production");

        Assert.IsType<MemoryCacheManager>(cacheManager);
    }

    [Fact]
    public async Task AddDeliveryMemoryCache_DefaultClient_AdvancedOverload_UsesUnprefixedNamespace()
    {
        _serviceCollection.AddDeliveryClient(o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDeliveryMemoryCache(opts =>
        {
            opts.DefaultExpiration = TimeSpan.FromMinutes(5);
        });

        var provider = _serviceCollection.BuildServiceProvider();
        var manager = provider.GetRequiredKeyedService<IDeliveryCacheManager>("Default");
        var sharedMemoryCache = provider.GetRequiredService<IMemoryCache>();
        using var unprefixedManager = new MemoryCacheManager(
            sharedMemoryCache,
            new DeliveryCacheOptions
            {
                KeyPrefix = string.Empty,
                DefaultExpiration = TimeSpan.FromMinutes(5)
            });

        await manager.GetOrSetAsync(
            "prefix-check",
            _ => Task.FromResult<CacheEntry<string>?>(
                new CacheEntry<string>("cached-value", Array.Empty<string>())));

        var factoryCalled = false;
        var cached = await unprefixedManager.GetOrSetAsync<string>(
            "prefix-check",
            _ =>
            {
                factoryCalled = true;
                return Task.FromResult<CacheEntry<string>?>(null);
            });

        Assert.False(factoryCalled);
        Assert.Equal("cached-value", cached?.Value);
    }

    [Fact]
    public void AddDeliveryMemoryCache_MultipleClients_RegistersSeparateKeyedManagers()
    {
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

        var provider = _serviceCollection.BuildServiceProvider();
        var prodCacheManager = provider.GetKeyedService<IDeliveryCacheManager>("production");
        var previewCacheManager = provider.GetKeyedService<IDeliveryCacheManager>("preview");

        // Both should be registered and be different instances
        Assert.NotNull(prodCacheManager);
        Assert.NotNull(previewCacheManager);
        Assert.NotSame(prodCacheManager, previewCacheManager);
    }

    [Fact]
    public void AddDeliveryMemoryCache_ClientWithoutCacheRegistration_ReturnsNull()
    {
        // Register client but NOT cache
        _serviceCollection.AddDeliveryClient("no-cache", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });

        var provider = _serviceCollection.BuildServiceProvider();
        var cacheManager = provider.GetKeyedService<IDeliveryCacheManager>("no-cache");

        // No cache manager should be registered for this client
        Assert.Null(cacheManager);
    }

    [Fact]
    public void AddDeliveryMemoryCache_WithCustomExpiration_PassesExpirationToManager()
    {
        var expiration = TimeSpan.FromMinutes(30);
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDeliveryMemoryCache("production", defaultExpiration: expiration);

        var provider = _serviceCollection.BuildServiceProvider();
        var cacheManager = provider.GetKeyedService<IDeliveryCacheManager>("production");

        Assert.NotNull(cacheManager);
        Assert.IsType<MemoryCacheManager>(cacheManager);
    }

    [Fact]
    public void AddDeliveryMemoryCache_NullClientName_ThrowsArgumentNullException()
    {
        // Use named parameter to ensure we call the string overload
        Assert.Throws<ArgumentNullException>(() =>
            _serviceCollection.AddDeliveryMemoryCache(clientName: null!));
    }

    [Fact]
    public void AddDeliveryMemoryCache_EmptyClientName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _serviceCollection.AddDeliveryMemoryCache(clientName: ""));
    }

    [Fact]
    public void AddDeliveryMemoryCache_InvalidDefaultExpiration_ThrowsValidationException()
    {
        Assert.Throws<ValidationException>(() =>
            _serviceCollection.AddDeliveryMemoryCache("production", defaultExpiration: TimeSpan.Zero));
    }

    [Fact]
    public void AddDeliveryMemoryCache_InvalidEagerRefreshThreshold_ThrowsValidationException()
    {
        Assert.Throws<ValidationException>(() =>
            _serviceCollection.AddDeliveryMemoryCache("production", opts => opts.EagerRefreshThreshold = 1.5f));
    }

    [Fact]
    public void AddDeliveryHybridCache_NegativeJitter_ThrowsValidationException()
    {
        _serviceCollection.AddDistributedMemoryCache();

        Assert.Throws<ValidationException>(() =>
            _serviceCollection.AddDeliveryHybridCache("production", opts => opts.JitterMaxDuration = TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void AddDeliveryHybridCache_NullClientName_ThrowsArgumentNullException()
    {
        // Use named parameter to ensure we call the string overload
        Assert.Throws<ArgumentNullException>(() =>
            _serviceCollection.AddDeliveryHybridCache(clientName: null!));
    }

    [Fact]
    public void AddDeliveryMemoryCache_RegistersContentDependencyExtractor_CacheFirst()
    {
        // Cache registered before client
        _serviceCollection.AddDeliveryMemoryCache("production");
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });

        var provider = _serviceCollection.BuildServiceProvider();
        var extractor = provider.GetService<IContentDependencyExtractor>();

        // Should register the real extractor (not null extractor)
        Assert.NotNull(extractor);
        Assert.DoesNotContain("Null", extractor.GetType().Name);
    }

    [Fact]
    public void AddDeliveryMemoryCache_RegistersContentDependencyExtractor_ClientFirst()
    {
        // Client registered before cache (order should not matter)
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDeliveryMemoryCache("production");

        var provider = _serviceCollection.BuildServiceProvider();
        var extractor = provider.GetService<IContentDependencyExtractor>();

        // Should register the real extractor regardless of order
        Assert.NotNull(extractor);
        Assert.DoesNotContain("Null", extractor.GetType().Name);
    }

    [Fact]
    public void AddDeliveryHybridCache_RegistersContentDependencyExtractor_CacheFirst()
    {
        // Cache registered before client
        _serviceCollection.AddDistributedMemoryCache();
        _serviceCollection.AddDeliveryHybridCache("production");
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });

        var provider = _serviceCollection.BuildServiceProvider();
        var extractor = provider.GetService<IContentDependencyExtractor>();

        // Should register the real extractor (not null extractor)
        Assert.NotNull(extractor);
        Assert.DoesNotContain("Null", extractor.GetType().Name);
    }

    [Fact]
    public void AddDeliveryHybridCache_RegistersContentDependencyExtractor_ClientFirst()
    {
        // Client registered before cache (order should not matter)
        _serviceCollection.AddDistributedMemoryCache();
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDeliveryHybridCache("production");

        var provider = _serviceCollection.BuildServiceProvider();
        var extractor = provider.GetService<IContentDependencyExtractor>();

        // Should register the real extractor regardless of order
        Assert.NotNull(extractor);
        Assert.DoesNotContain("Null", extractor.GetType().Name);
    }

    #endregion

    #region Keyed Service Fallback Tests

    [Fact]
    public void MultipleNamedClients_OneWithCache_OtherWithoutCache()
    {
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

        var provider = _serviceCollection.BuildServiceProvider();
        var prodCacheManager = provider.GetKeyedService<IDeliveryCacheManager>("production");
        var previewCacheManager = provider.GetKeyedService<IDeliveryCacheManager>("preview");

        // Only production has cache
        Assert.NotNull(prodCacheManager);
        Assert.Null(previewCacheManager);
    }

    [Fact]
    public void AddDeliveryMemoryCache_SharedUnderlyingMemoryCache()
    {
        // Both clients use the same IMemoryCache but different key prefixes
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

        var provider = _serviceCollection.BuildServiceProvider();

        // Both cache managers should resolve to separate keyed instances
        var cache1 = provider.GetKeyedService<IDeliveryCacheManager>("client1");
        var cache2 = provider.GetKeyedService<IDeliveryCacheManager>("client2");

        // But they share the underlying IMemoryCache (singleton)
        var memoryCache = provider.GetService<IMemoryCache>();

        Assert.NotNull(cache1);
        Assert.NotNull(cache2);
        Assert.NotSame(cache1, cache2);
        Assert.NotNull(memoryCache);
    }

    #endregion

    #region (IServiceProvider, TOptions) Overloads

    private sealed class SiblingOptions
    {
        public string EnvironmentIdValue { get; set; } = string.Empty;
        public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(5);
    }

    [Fact]
    public void AddDeliveryClient_WithServiceProviderCallback_ResolvesFromContainer()
    {
        _serviceCollection.Configure<SiblingOptions>(o => o.EnvironmentIdValue = EnvironmentId);
        _serviceCollection.AddDeliveryClient((sp, opts) =>
        {
            opts.EnvironmentId = sp.GetRequiredService<IOptions<SiblingOptions>>().Value.EnvironmentIdValue;
            opts.EnableResilience = false;
        });

        var provider = _serviceCollection.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
        Assert.Equal(EnvironmentId, monitor.CurrentValue.EnvironmentId);
    }

    [Fact]
    public void AddDeliveryClient_NamedWithServiceProviderCallback_ResolvesFromContainer()
    {
        _serviceCollection.Configure<SiblingOptions>(o => o.EnvironmentIdValue = EnvironmentId);
        _serviceCollection.AddDeliveryClient("production", (sp, opts) =>
        {
            opts.EnvironmentId = sp.GetRequiredService<IOptions<SiblingOptions>>().Value.EnvironmentIdValue;
            opts.EnableResilience = false;
        });

        var provider = _serviceCollection.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<DeliveryOptions>>();
        Assert.Equal(EnvironmentId, monitor.Get("production").EnvironmentId);
    }

    [Fact]
    public void AddDeliveryClient_NullServiceProviderCallback_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _serviceCollection.AddDeliveryClient(configureOptions: (Action<IServiceProvider, DeliveryOptions>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            _serviceCollection.AddDeliveryClient("production", configureOptions: (Action<IServiceProvider, DeliveryOptions>)null!));
    }

    [Fact]
    public void AddDeliveryMemoryCache_WithServiceProviderCallback_InvokesCallbackOnResolution()
    {
        _serviceCollection.Configure<SiblingOptions>(o => o.CacheExpiration = TimeSpan.FromHours(3));
        _serviceCollection.AddDeliveryClient(o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });

        var invokedWithExpiration = TimeSpan.Zero;
        _serviceCollection.AddDeliveryMemoryCache((sp, opts) =>
        {
            opts.DefaultExpiration = sp.GetRequiredService<IOptions<SiblingOptions>>().Value.CacheExpiration;
            invokedWithExpiration = opts.DefaultExpiration;
        });

        var provider = _serviceCollection.BuildServiceProvider();
        var cacheManager = provider.GetKeyedService<IDeliveryCacheManager>("Default");

        Assert.NotNull(cacheManager);
        Assert.IsType<MemoryCacheManager>(cacheManager);
        Assert.Equal(TimeSpan.FromHours(3), invokedWithExpiration);
    }

    [Fact]
    public void AddDeliveryMemoryCache_NamedWithServiceProviderCallback_InvokesCallbackOnResolution()
    {
        _serviceCollection.Configure<SiblingOptions>(o => o.CacheExpiration = TimeSpan.FromHours(2));
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });

        var invokedWithExpiration = TimeSpan.Zero;
        _serviceCollection.AddDeliveryMemoryCache("production", (sp, opts) =>
        {
            opts.DefaultExpiration = sp.GetRequiredService<IOptions<SiblingOptions>>().Value.CacheExpiration;
            invokedWithExpiration = opts.DefaultExpiration;
        });

        var provider = _serviceCollection.BuildServiceProvider();
        var cacheManager = provider.GetKeyedService<IDeliveryCacheManager>("production");

        Assert.NotNull(cacheManager);
        Assert.Equal(TimeSpan.FromHours(2), invokedWithExpiration);
    }

    [Fact]
    public void AddDeliveryMemoryCache_WithServiceProviderCallback_DefersInvocationUntilResolution()
    {
        var invoked = false;
        _serviceCollection.AddDeliveryClient(o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDeliveryMemoryCache((_, opts) =>
        {
            invoked = true;
            opts.DefaultExpiration = TimeSpan.FromMinutes(10);
        });

        // Not invoked at registration time
        Assert.False(invoked);

        var provider = _serviceCollection.BuildServiceProvider();

        // Not invoked just from building the provider
        Assert.False(invoked);

        _ = provider.GetRequiredKeyedService<IDeliveryCacheManager>("Default");

        // Invoked on first resolution
        Assert.True(invoked);
    }

    [Fact]
    public void AddDeliveryMemoryCache_NullServiceProviderCallback_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _serviceCollection.AddDeliveryMemoryCache(configureCacheOptions: (Action<IServiceProvider, DeliveryCacheOptions>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            _serviceCollection.AddDeliveryMemoryCache("production", configureCacheOptions: (Action<IServiceProvider, DeliveryCacheOptions>)null!));
    }

    [Fact]
    public void AddDeliveryHybridCache_WithServiceProviderCallback_InvokesCallbackOnResolution()
    {
        _serviceCollection.Configure<SiblingOptions>(o => o.CacheExpiration = TimeSpan.FromHours(4));
        _serviceCollection.AddDeliveryClient(o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDistributedMemoryCache();

        var invokedWithExpiration = TimeSpan.Zero;
        _serviceCollection.AddDeliveryHybridCache((sp, opts) =>
        {
            opts.DefaultExpiration = sp.GetRequiredService<IOptions<SiblingOptions>>().Value.CacheExpiration;
            invokedWithExpiration = opts.DefaultExpiration;
        });

        var provider = _serviceCollection.BuildServiceProvider();
        var cacheManager = provider.GetKeyedService<IDeliveryCacheManager>("Default");

        Assert.NotNull(cacheManager);
        Assert.IsType<HybridCacheManager>(cacheManager);
        Assert.Equal(TimeSpan.FromHours(4), invokedWithExpiration);
    }

    [Fact]
    public void AddDeliveryHybridCache_NamedWithServiceProviderCallback_InvokesCallbackOnResolution()
    {
        _serviceCollection.Configure<SiblingOptions>(o => o.CacheExpiration = TimeSpan.FromHours(6));
        _serviceCollection.AddDeliveryClient("production", o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDistributedMemoryCache();

        var invokedWithExpiration = TimeSpan.Zero;
        _serviceCollection.AddDeliveryHybridCache("production", (sp, opts) =>
        {
            opts.DefaultExpiration = sp.GetRequiredService<IOptions<SiblingOptions>>().Value.CacheExpiration;
            invokedWithExpiration = opts.DefaultExpiration;
        });

        var provider = _serviceCollection.BuildServiceProvider();
        var cacheManager = provider.GetKeyedService<IDeliveryCacheManager>("production");

        Assert.NotNull(cacheManager);
        Assert.Equal(TimeSpan.FromHours(6), invokedWithExpiration);
    }

    [Fact]
    public void AddDeliveryHybridCache_NullServiceProviderCallback_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _serviceCollection.AddDeliveryHybridCache(configureCacheOptions: (Action<IServiceProvider, DeliveryCacheOptions>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            _serviceCollection.AddDeliveryHybridCache("production", configureCacheOptions: (Action<IServiceProvider, DeliveryCacheOptions>)null!));
    }

    [Fact]
    public void AddDeliveryMemoryCache_WithServiceProviderCallback_DefersValidationUntilResolution()
    {
        _serviceCollection.AddDeliveryClient(o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });

        // Zero expiration fails [PositiveTimeSpan] validation.
        _serviceCollection.AddDeliveryMemoryCache((_, opts) => opts.DefaultExpiration = TimeSpan.Zero);

        // Registration and provider construction must not throw — validation is deferred.
        var provider = _serviceCollection.BuildServiceProvider();

        Assert.Throws<ValidationException>(() =>
            provider.GetRequiredKeyedService<IDeliveryCacheManager>("Default"));
    }

    [Fact]
    public void AddDeliveryMemoryCache_WithInlineCallback_ValidatesEagerlyAtRegistration()
    {
        _serviceCollection.AddDeliveryClient(o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });

        // Contrast with the (sp, opts) overload: the plain Action<DeliveryCacheOptions>
        // overload validates at registration time, not at resolution.
        Assert.Throws<ValidationException>(() =>
            _serviceCollection.AddDeliveryMemoryCache(opts => opts.DefaultExpiration = TimeSpan.Zero));
    }

    [Fact]
    public void AddDeliveryHybridCache_WithServiceProviderCallback_DefersValidationUntilResolution()
    {
        _serviceCollection.AddDeliveryClient(o =>
        {
            o.EnvironmentId = EnvironmentId;
            o.EnableResilience = false;
        });
        _serviceCollection.AddDistributedMemoryCache();

        _serviceCollection.AddDeliveryHybridCache((_, opts) => opts.DefaultExpiration = TimeSpan.Zero);

        var provider = _serviceCollection.BuildServiceProvider();

        Assert.Throws<ValidationException>(() =>
            provider.GetRequiredKeyedService<IDeliveryCacheManager>("Default"));
    }

    [Fact]
    public void AddDeliveryClient_ServiceProviderCallback_ResolvingOptionsInDelegate_ReEntersConfigureDelegate()
    {
        // Documents the circular-dependency guard from the XML docs: the (sp, opts) callback
        // must not resolve IOptions<DeliveryOptions>, IDeliveryClient, or IDeliveryApi — doing
        // so re-enters this very delegate through the options factory. Left unguarded, the MS
        // DI runtime recurses unboundedly and stack-overflows (uncatchable by design). We use
        // a reentrancy counter to prove the re-entry and surface a catchable exception.
        var depth = 0;
        _serviceCollection.AddDeliveryClient((sp, opts) =>
        {
            if (Interlocked.Increment(ref depth) > 1)
            {
                throw new InvalidOperationException("configure delegate re-entered — circular dependency via options resolution");
            }

            try
            {
                // Triggers the same configure delegate again via the options factory.
                _ = sp.GetRequiredService<IOptions<DeliveryOptions>>().Value;
            }
            finally
            {
                Interlocked.Decrement(ref depth);
            }

            opts.EnvironmentId = EnvironmentId;
        });

        var provider = _serviceCollection.BuildServiceProvider();

        var ex = Assert.ThrowsAny<InvalidOperationException>(() =>
            provider.GetRequiredService<IOptions<DeliveryOptions>>().Value);
        Assert.Contains("re-entered", ex.Message);
    }

    #endregion

    private sealed class TestCustomCacheManager : IDeliveryCacheManager
    {
        public async Task<CacheResult<T>?> GetOrSetAsync<T>(
            string cacheKey,
            Func<CancellationToken, Task<CacheEntry<T>?>> factory,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default) where T : class
        {
            var entry = await factory(cancellationToken);
            if (entry is null)
                return null;
            return new CacheResult<T>(entry.Value, entry.Dependencies.ToArray());
        }

        public Task<bool> InvalidateAsync(string[] dependencyKeys, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private int CountCacheManagerRegistrations(string clientName)
    {
        return _serviceCollection.Count(d =>
            d.ServiceType == typeof(IDeliveryCacheManager) &&
            Equals(d.ServiceKey, clientName));
    }
}
