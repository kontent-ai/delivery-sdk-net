// See https://aka.ms/new-console-template for more information

using System.Configuration;
using DeliverySDKWithAutofac;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Extensions;
using Kentico.Kontent.Delivery.Extensions.DependencyInjection;
using Kentico.Kontent.Urls.Delivery.QueryParameters;
using Kentico.Kontent.Urls.Delivery.QueryParameters.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.WriteLine("App starting");

const string ClientA = "ClientA";
const string ClientAProjectId = "f249eb83-18fd-01b8-2db7-c561bcb1ed1e";


const string ClientB = "ClientB";
const string ClientBProjectId = "b259760f-81c5-013a-05e7-69efb4b954e5";

using IHost host = Host
    .CreateDefaultBuilder(args)
    .ConfigureServices((config, services) =>
    {
        // services.AddAutofac();

        // Deprecate all autofac implementation and 
        // Mind this wiki to add info https://github.com/Kentico/kontent-delivery-sdk-net/wiki/Accessing-Data-From-Multiple-Projects

        // For 1 delivery client
        // TODO 312 deprecate method with name attribute
        // TODO 312 Create new overload without the name 
        // services.AddDeliveryClient(
        //     builder =>
        //     builder.WithProjectId(ClientAProjectId)
        //         .UseProductionApi()
        //         .Build());


        // For multiple clients
        // TODO 312 introduce AddDeliveryClientFactory extension method
        // TODO 312 Adjust DeliveryClientFactory 
        // TODO 312 deprecate Name client factoy implementations (of IdeliveryClientFactory) => NamedDeliveryClientFactory, NamedDeliveryClientCacheFactory
        // TODO 312 DeliveryClientFactoryBuilder->DeliveryClientBuilder->DeliveryOptionsBuilder

        services.AddDeliveryClientFactory(
            factoryBuilder => factoryBuilder
                .AddDeliveryClient(
                    ClientA,
                    deliveryOptionBuilder => deliveryOptionBuilder
                        .WithProjectId(ClientAProjectId)
                        .UseProductionApi()
                        .Build(),
                    optionalClientSetup =>
                        optionalClientSetup.WithTypeProvider(new ProjectAProvider())
                )
                .AddDeliveryClient(
                    ClientB,
                    deliveryOptionBuilder => deliveryOptionBuilder
                        .WithProjectId(ClientBProjectId)
                        .UseProductionApi()
                        .Build(),
                    optionalClientSetup =>
                        optionalClientSetup.WithTypeProvider(new ProjectBProvider())
                )
                .AddDeliveryClient(
                    "C",
                    _ =>
                    {
                        var options = new DeliveryOptions();
                        config.Configuration.GetSection("MultipleDeliveryOptions:C").Bind(options);
                        return options;
                    }
                )
                .AddDeliveryClient(
                    "D",
                    _ =>
                    {
                        var options = new DeliveryOptions();
                        config.Configuration.GetSection("MultipleDeliveryOptions:D").Bind(options);
                        return options;
                    }
                )
                .Build()
            );
    }).Build();

// For multiple clients from appsettings.json
// services.AddDeliveryClientFactory(config.getSection <Dictionary<string, DeliveryOptions>>("MultipleDeliveryOptions"));

// Validate with different HttpClients
// https://github.com/Kentico/kontent-delivery-sdk-net/wiki/Registering-the-DeliveryClient-to-the-IServiceCollection-in-ASP.NET-Core#registering-multiple-clients
// validate with HttpCLientFactory
// Mind the support netstandard 2.0 + net 6.0 - probably split out to separate issue depending on netstandard 2.0 support drop


var deliveryClientFactory = host.Services.GetRequiredService<IDeliveryClientFactory>();

var itemsA = await deliveryClientFactory.Get(ClientA)
    .GetItemsAsync<Article>(new SystemTypeEqualsFilter("article"), new DepthParameter(2));

var itemsB = await deliveryClientFactory.Get(ClientB)
    .GetItemsAsync<Movie>(new SystemTypeEqualsFilter("movie"), new DepthParameter(2));

foreach (var item in itemsA.Items)
{
    if (item is Article article)
    {
        Console.WriteLine($"Item '{article.System.Codename}' is of '{item.GetType()}' type");

        foreach (var writer in article.Writers)
        {
            if (writer == null)
            {
                Console.WriteLine($"Item is null");
            }
            else
            {
                Console.WriteLine($"Item '{writer.System.Codename}' is of '{writer.GetType()}' type");
            }
        }
    }
    else if (item == null)
    {
        Console.WriteLine($"Item is null");
    }
    else
    {
        Console.WriteLine($"Invalid type for item");
    }
}

Console.WriteLine("======================");

foreach (var item in itemsB.Items)
{
    if (item is Movie movie)
    {
        Console.WriteLine($"Item '{movie.System.Codename}' is of '{item.GetType()}' type");

        foreach (var actor in movie.Stars)
        {
            if (actor == null)
            {
                Console.WriteLine($"Item is null");
            }
            else
            {
                Console.WriteLine($"Item '{actor.System.Codename}' is of '{actor.GetType()}' type");
            }
        }
    }
    else if (item == null)
    {
        Console.WriteLine($"Item is null");
    }
    else
    {
        Console.WriteLine($"Invalid type for item");
    }
}

Console.WriteLine("Finished");