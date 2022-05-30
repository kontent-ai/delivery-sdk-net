﻿// See https://aka.ms/new-console-template for more information

using Autofac;
using Autofac.Extensions.DependencyInjection;
using DeliverySDKWithAutofac;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Extensions.DependencyInjection;
using Kentico.Kontent.Urls.Delivery.QueryParameters;
using Kentico.Kontent.Urls.Delivery.QueryParameters.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AutofacServiceProviderFactory = Autofac.Extensions.DependencyInjection.AutofacServiceProviderFactory;

Console.WriteLine("App starting");

const string ClientA = "ClientA";
const string ClientAProjectId = "f249eb83-18fd-01b8-2db7-c561bcb1ed1e";


const string ClientB = "ClientB";
const string ClientBProjectId = "b259760f-81c5-013a-05e7-69efb4b954e5";

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((config, services) =>
    {
        // services.AddAutofac();

        // Deprecate all autofac implementation and 
        // Mind this wiki to add info https://github.com/Kentico/kontent-delivery-sdk-net/wiki/Accessing-Data-From-Multiple-Projects

        // For 1 delivery client
        // TODO deprecate method with name attribute
        // Create new overload without the name 
        // services.AddDeliveryClient(
        //     builder =>
        //     builder.WithProjectId(ClientAProjectId)
        //         .UseProductionApi()
        //         .Build());

        // For multiple clients
        // TODO introduce AddDeliveryClientFactory extension methos
        // TODO Adjust DeliveryClientFactory 
        // TODO deprecate Name client factoy implementations (of IdeliveryClientFactory) => NamedDeliveryClientFactory, NamedDeliveryClientCacheFactory
        // services.AddDeliveryClientFactory(builder => builder
        // .AddDeliveryClient("A", builder => builder.WithProjectId(ClientAProjectId)
        //         .UseProductionApi()
        //         .Build())
        // .AddDeliveryClient("B", builder => builder.WithProjectId(ClientBProjectId)
        //         .UseProductionApi()
        //         .Build()));

        // For multiple clients from appsettings.json
        // services.AddDeliveryClientFactory(config.getSection <Dictionary<string, DeliveryOptions>>("MultipleDeliveryOptions"));
        
        // Validate with different HttpClients
        // https://github.com/Kentico/kontent-delivery-sdk-net/wiki/Registering-the-DeliveryClient-to-the-IServiceCollection-in-ASP.NET-Core#registering-multiple-clients
        // validate with HttpCLientFactory
        // Mind the support netstandard 2.0 + net 6.0 - probably split out to separate issue depending on netstandard 2.0 support drop
    })
    .Build();


var deliveryClientFactory = host.Services.GetRequiredService<IDeliveryClientFactory>();

var itemsA = await deliveryClientFactory.Get("A")
    .GetItemsAsync<Article>(new SystemTypeEqualsFilter("article"), new DepthParameter(2));

var itemsB = await deliveryClientFactory.Get("B")
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