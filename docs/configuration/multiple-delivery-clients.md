# Register Multiple Delivery Clients

> ⚠️ Mind [because of the issue with the named clients implementation](https://github.com/kontent-ai/delivery-sdk-net/issues/312) we decided to deprecate `AutofacServiceProviderFactory` use `MultipleDeliveryClientFactory` instead.

Sometimes, it's handy to register multiple `IDeliveryClient`s with different configurations (e.g. while accessing different projects, accessing secured and non-secured data at once, or accessing preview and production data at the same time). In those cases, you can take advantage of multiple client registration using factory pattern.

If you wish to implement support for a DI container of your choice, jump to the [Extending named services support](#extending-named-services-support) section.

## Using the default .NET implementation of the IMultipleDeliveryClientFactory

### Installing the NuGet packages

Advanced registration scenarios are handled by the [Kontent.Ai.Delivery.Extensions.DependencyInjection](https://www.nuget.org/packages/Kontent.Ai.Delivery.Extensions.DependencyInjection) NuGet package. You'll need to install it first:

```sh
Install-Package Kontent.Ai.Delivery.Extensions.DependencyInjection
```

### Registering the factory

The SDK provides extension methods upon the `IServiceCollection` that allow registering delivery client factory builder. The builder is used to configure the factory and register the delivery client instances.

```csharp
public class Startup
{
    // ...

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMultipleDeliveryClientFactory
        (
            factoryBuilder => factoryBuilder
                .AddDeliveryClient
                (
                    "projectA",
                    deliveryOptionBuilder => deliveryOptionBuilder
                        .WithProjectId("<A_PROJECT_ID>")
                        .UseProductionApi()
                        .Build()
                    optionalClientSetup =>
                        optionalClientSetup.WithTypeProvider(new ProjectAProvider())
                )
                .Build()
        );
    }
}
```

### Registering multiple type providers

If you're accessing two completely different projects, chances are they have a different content model and therefore the generated models for content types will differ. Extend the Startup class as follows:

```csharp
public class Startup
{
    // ...

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMultipleDeliveryClientFactory
        (
            factoryBuilder => factoryBuilder
                .AddDeliveryClient
                (
                    "projectA",
                    deliveryOptionBuilder => deliveryOptionBuilder
                        .WithProjectId("<A_PROJECT_ID>")
                        .UseProductionApi()
                        .Build(),
                    optionalClientSetup =>
                        optionalClientSetup.WithTypeProvider(new ProjectAProvider())
                )
                .AddDeliveryClient(
                    "projectB",
                    deliveryOptionBuilder => deliveryOptionBuilder
                        .WithProjectId("<B_PROJECT_ID>")
                        .UseProductionApi()
                        .Build(),
                    optionalClientSetup =>
                        optionalClientSetup.WithTypeProvider(new ProjectBProvider())
                )
                .Build()
        );
    }
}
```

### Registering Cached client

> To be able to use the [.NET SDK caching layer](../retrieving-data/caching.md), you need to install `Kontent.Ai.Delivery.Caching` package.

If you want to use the cached client, you can use the `AddCachedDeliveryClient` method instead of `AddDeliveryClient`

```csharp
public class Startup
{
    // ...

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMultipleDeliveryClientFactory
        (
                factoryBuilder => factoryBuilder
                    .AddDeliveryClientCache
                    (
                        "projectA"
                        deliveryOptionBuilder => deliveryOptionBuilder
                            .WithProjectId(ClientAProjectId)
                            .UseProductionApi()
                            .Build(),
                        CacheManagerFactory.Create(
                            new MemoryCache(new MemoryCacheOptions()),
                            Options.Create(new DeliveryCacheOptions
                            {
                                CacheType = CacheTypeEnum.Memory
                            })),
                        optionalClientSetup =>
                            optionalClientSetup.WithTypeProvider(new ProjectAProvider())
                    )
                    .Build()
        );
    }
}
```

### Load the configuration dynamically

It is of course possible to load the configuration from `Configuration` object (ie. from `appsettings.json`).

```csharp
public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfigurationRoot Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMultipleDeliveryClientFactory
        (
            factoryBuilder => factoryBuilder
                .AddDeliveryClient
                (
                    "projectA",
                    _ =>
                    {
                        var options = new DeliveryOptions();
                        config.Configuration.GetSection("MultipleDeliveryOptions:ProjectA")
                            .Bind(options);
                        return options;
                    },
                    optionalClientSetup =>
                        optionalClientSetup.WithTypeProvider(new ProjectAProvider())
                )
                .Build()
        );
    }
}
```

### Resolving the delivery client

For resolving multiple clients, inject the [IDeliveryClientFactory](https://github.com/kontent-ai/delivery-sdk-net/Kontent.Ai.Delivery.Abstractions/IDeliveryClientFactory.cs), which is registered in the DI container.

```csharp
public class HomeController : Controller
{
    private IDeliveryClient _deliveryClient;

    public HomeController(IDeliveryClientFactory deliveryClientFactory)
    {
        _deliveryClient = deliveryClientFactory.Get("projectA");
    }
}
```

## Extending named services support

In case you want to use a different implementation for multiple client factory t, feel free to create your own implementation of `IDeliveryClientFactory` (ideally with the implementation of a `IMultipleDeliveryClientFactoryBuilder`)  and submit a pull request to this repository.
