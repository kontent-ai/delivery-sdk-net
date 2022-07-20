> ⚠️ Mind [the issue with the combination of the named `TypeProvider` and `Modelprovider`](https://github.com/Kentico/kontent-delivery-sdk-net/issues/312) - see [the workaround for multple clients with different Type providers](https://github.com/Kentico/kontent-delivery-sdk-net/issues/312#issuecomment-1138139987)

Sometimes, it's handy to register multiple `IDeliveryClient`s with different configurations (e.g. while accessing different projects, accessing secured and non-secured data at once, or accessing preview and production data at the same time). In those cases, you can take advantage of named clients.
This SDK contains a default implementation of named clients relying on Autofac's [Named and Keyed Services](https://autofaccn.readthedocs.io/en/latest/advanced/keyed-services.html). If you wish to implement support for a DI container of your choice, jump to the [Extending named services support](#extending-named-services-support) section.

# Using the default Autofac implementation

## Installing the NuGet packages
Advanced registration scenarios are handled by the [Kentico.Kontent.Delivery.Extensions.DependencyInjection](https://www.nuget.org/packages/Kentico.Kontent.Delivery.Extensions.DependencyInjection) NuGet package. You'll need to install the following packages:
```
PM> Install-Package Kentico.Kontent.Delivery.Extensions.DependencyInjection
PM> Install-Package Autofac.Extensions.DependencyInjection
```

## Adding Autofac to the hosting pipeline
To enable Autofac DI resolution, you need to [add Autofac to your hosting pipeline](https://autofaccn.readthedocs.io/en/latest/integration/aspnetcore.html#asp-net-core-3-0-and-generic-hosting) as follows:

```csharp
public class Program
{
     public static IHostBuilder CreateHostBuilder(string[] args) =>
              Host.CreateDefaultBuilder(args)
                  .UseServiceProviderFactory(new AutofacServiceProviderFactory()) // Add this line
                  .ConfigureWebHostDefaults(webBuilder =>
                  {
                      webBuilder.UseStartup<Startup>();
                  });
}
```

## Registering multiple named DeliveryClients

The SDK provides extension methods upon the `IServiceCollection` that allow registering clients indexed by name.

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
        services.AddDeliveryClient("projectA", Configuration, "ProjectAOptions", NamedServiceProviderType.Autofac);
        services.AddDeliveryClient("projectB", Configuration, "ProjectBOptions", NamedServiceProviderType.Autofac);
    }
}
```

For resolving named clients, inject the [IDeliveryClientFactory](https://github.com/Kentico/kontent-delivery-sdk-net/blob/master/Kentico.Kontent.Delivery.Abstractions/IDeliveryClientFactory.cs), which is registered in the DI container.

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

## Registering multiple type providers
If you're accessing two completely different projects, chances are they have a different content model and therefore the generated models for content types will differ. Extend the Startup class as follows:

```csharp
public class Startup
{
        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterType<ProjectACustomTypeProvider>().Named<ITypeProvider>("projectA");
            builder.RegisterType<ProjectBCustomTypeProvider>().Named<ITypeProvider>("projectB");
        }
}
```

More details in [Autofac's docs](https://autofaccn.readthedocs.io/en/latest/integration/aspnetcore.html#startup-class).

# Extending named services support
In case you want to use a DI container other than Autofac, feel free to create your own implementation of `INamedServiceProvider` (in `Kentico.Kontent.Delivery.Extensions.DependencyInjection`) and submit a pull request to this repository.
This implementation then needs to be registered in `NamedServiceProviderType` and [`ServiceCollectionExtensions`](https://github.com/Kentico/kontent-delivery-sdk-net/blob/master/Kentico.Kontent.Delivery.Extensions.DependencyInjection/Extensions/ServiceCollectionExtensions.cs#L133).
We'll be happy to work with you to add support for your favorite DI container.