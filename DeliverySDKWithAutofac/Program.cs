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
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureContainer<ContainerBuilder>(builder =>
    {
        builder.RegisterType<ProjectAProvider>().Named<ITypeProvider>(ClientA);
        builder.RegisterType<ProjectBProvider>().Named<ITypeProvider>(ClientB);
    })
    .ConfigureServices((_, services) =>
    {
        services.AddAutofac();

        services.AddDeliveryClient(
            ClientA, builder =>
            builder.WithProjectId(ClientAProjectId)
                .UseProductionApi()
                .Build(),
            NamedServiceProviderType.Autofac);

        services.AddDeliveryClient(
            ClientB, builder =>
                builder.WithProjectId(ClientBProjectId)
                    .UseProductionApi()
                    .Build(),
            NamedServiceProviderType.Autofac);
    })
    .Build();


var deliveryClientFactory = host.Services.GetRequiredService<IDeliveryClientFactory>();

var clientA = deliveryClientFactory.Get(ClientA);
var clientB = deliveryClientFactory.Get(ClientB);

var itemsA = await clientA
    .GetItemsAsync<Article>(new SystemTypeEqualsFilter("article"), new DepthParameter(2));

var itemsB = await clientB
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