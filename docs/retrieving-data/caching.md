To add an extra layer of caching, use the [Kontent.Ai.Delivery.Caching](https://www.nuget.org/packages/Kontent.Ai.Delivery.Caching) NuGet package. The caching is facilitated by a decorated `IDeliveryClient` instance that uses an implementation of the `IDeliveryCacheManager` interface to retrieve items.

There are two implementations of the `IDeliveryCacheManager` interface:
- `MemoryCacheManager` - based on the [`IMemoryCache`](https://docs.microsoft.com/en-us/aspnet/core/performance/caching/memory)
- `DistributedCacheManager` - based on the [`IDistributedCache`](https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed)

Unless specified otherwise, both implementations use in-memory implementations (`MemoryCache`, `MemoryDistributedCache`) of the respective interfaces.

## Registration using DI

Use this approach to register the caching package for all `IDeliveryClient` instances. (Order does matter.)

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddDeliveryClient(Configuration);
    services.AddDeliveryClientCache(new DeliveryCacheOptions());
}
```

### Selective registration
Use this approach if you wish to register the cache for a single [named `IDeliveryClient` instance](../configuration/multiple-delivery-clients.md). Please not that the order of the method calls does matter.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddDeliveryClient("production", Configuration, "DeliveryOptions1");
    services.AddDeliveryClientCache("production", new DeliveryCacheOptions());
}
```

## DeliveryCacheOptions

- `CacheType` - currently allows to select from [`Memory` and `Distributed`](../../Kontent.Ai.Delivery.Caching/CacheTypeEnum.cs)
- `DefaultExpiration` - [sliding expiration](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.memorycacheentryextensions.setslidingexpiration) time - how long the cache entry can be inactive (e.g. not accessed) before it will be removed
- `StaleContentExpiration ` - [absolute expiration](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.cacheentryextensions.setabsoluteexpiration) (timespan relative to now) for content which is [yet to be refreshed on the CDN](https://github.com/kontent-ai/boilerplate-net/issues/94#issuecomment-602688995)
- `DistributedCacheResilientPolicy ` - determines which resilient policy should be used when [`distributed cache`](https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed) is not available. Currently allows to select from [`Crash` and `FallbackToApi`](../../Kontent.Ai.Delivery.Caching/DistributedCacheResilientPolicy.cs)

## Distributed caching - example
This example shows how to use Redis cache on a local windows machine.

1. Install the [`Microsoft.Extensions.Caching.StackExchangeRedis`](https://www.nuget.org/packages/Microsoft.Extensions.Caching.StackExchangeRedis/) NuGet package.
2. Spin up an [Azure Cache for Redis](https://azure.microsoft.com/en-us/services/cache/) or install the [Windows port of Redis](https://github.com/tporadowski/redis/releases)
3. Adjust `Startup.cs`
```csharp
// First, add Azure Redis
services.AddStackExchangeRedisCache(options =>
{
    // Copy the connection string from the Access key tab in Azure Portal
    options.Configuration = "<your_instance>.redis.cache.windows.net:6380,password=<your_pwd>,ssl=True,abortConnect=False";
    options.InstanceName = "SampleInstance";
});

// Or local Redis cache
//services.AddStackExchangeRedisCache(options =>
//{
//    options.Configuration = "localhost";
//    options.InstanceName = "SampleInstance";
//});

// Second, add a DeliveryClient
services.AddDeliveryClientCache(new DeliveryCacheOptions()
{
    CacheType = CacheTypeEnum.Distributed
});
```
> You can optionally register [`ILoggerFactory` implementation](../configuration/dependency-injection.md). Logger will have effect only when `distributed cache` is used and [`FallbackToApi`](../../Kontent.Ai.Delivery.Caching/DistributedCacheResilientPolicy.cs) option is chosen for `DistributedCacheResilientPolicy` parameter of `DeliveryCacheOptions`. In this case information message will be logged when `distributed cache` is not available.

Read more in [Microsoft docs](https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed).

## Usage without DI

Use this approach to register the caching package for a specific `DeliveryClient` instance.

```csharp
   var client = DeliveryClientBuilder.WithEnvironmentId("<ENVIRONMENT_ID>").Build();           
   var cacheOptions = Options.Create(new DeliveryCacheOptions() { DefaultExpiration = new TimeSpan(2, 0, 0) }) ;
   var memoryOptions = Options.Create(new MemoryCacheOptions());
   var cachedClient = new DeliveryClientCache(CacheManagerFactory.Create(new MemoryCache(memoryOptions), cacheOptions), client);
```
> `CacheManagerFactory.Create` method for the [`IDistributedCache`](https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed) has optional `ILoggerFactory loggerFactory` parameter, which has the same impact, as described for `services.AddDeliveryClientCache` method.

## Cache eviction / cache item invalidation

### `MemoryCacheManager`
The default implementation of the `IMemoryCache` cache contains sophisticated logic for creating cache dependencies based on [linked items](https://docs.kontent.ai/tutorials/develop-apps/get-content/linked-items-and-subpages). This means that one can invalidate cache items not only by their own keys or keys of the collections containing them but also by the keys of their dependencies.

Explore the [.NET Boilerplate](https://github.com/kontent-ai/boilerplate-net/src/content/Kontent.Ai.Boilerplate/Areas/WebHooks/Controllers/WebhooksController.cs) to see how to invalidate cache items by cache dependencies using webhooks.

### `DistributedCacheManager`

Unlike to `IMemoryCache`, `IDistributedCache` does not support [expiration tokens](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.memorycacheentryextensions.addexpirationtoken?view=dotnet-plat-ext-3.1). More on that [here](https://www.devtrends.co.uk/blog/custom-response-caching-in-asp.net-core-with-cache-invalidation) and [here](https://www.devtrends.co.uk/blog/a-guide-to-caching-in-asp.net-core).

> DistributedCacheEntryOptions offers absolute and sliding expiration much like MemoryCacheEntryOptions but token based expiration is absent. This makes adding cache dependencies much more of a challenge and you will need to roll your own implementation if you need this functionality.

So for cache eviction, one can only use like keys generated by `CacheHelpers.GetItemKey()` and `GetItemsKey()` but not `GetItemsDependencyKey()`. (The same logic applies to Types, Elements, and Taxonomies.) Methods that are supposed to work fine with the distributed cache can easily be spotted in [tests](https://github.com/kontent-ai/delivery-sdk-net/Kontent.Ai.Delivery.Caching.Tests/DeliveryClientCacheTests.cs) by searching for those marked with `[InlineData(CacheTypeEnum.Distributed)]`.

Please not that it's also not possible to wipe the whole cache (which is, in a way, [possible](https://stackoverflow.com/a/45543023/1332034) with the `IMemoryCache`) and therefore, for cache invalidation, one can only rely on [absolute or sliding expiration](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.distributed.distributedcacheentryoptions?view=dotnet-plat-ext-3.1) if the cached keys.


## Customizing caching

You can also provide your own version of the caching mechanism by implementing the [`IDeliveryCacheManager`](https://github.com/kontent-ai/delivery-sdk-net/Kontent.Ai.Delivery.Abstractions/IDeliveryCacheManager.cs) interface.