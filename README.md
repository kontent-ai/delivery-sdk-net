# Kentico Kontent Delivery .NET SDK

[![Build status](https://ci.appveyor.com/api/projects/status/3m3q2ads2y43bh9o/branch/master?svg=true)](https://ci.appveyor.com/project/kentico/deliver-net-sdk/branch/master)
[![CircleCI](https://circleci.com/gh/Kentico/kontent-delivery-sdk-net.svg?style=shield)](https://circleci.com/gh/Kentico/kontent-delivery-sdk-net)
[![codecov](https://codecov.io/gh/Kentico/kontent-delivery-sdk-net/branch/master/graph/badge.svg)](https://codecov.io/gh/Kentico/kontent-delivery-sdk-net)
[![Stack Overflow](https://img.shields.io/badge/Stack%20Overflow-ASK%20NOW-FE7A16.svg?logo=stackoverflow&logoColor=white)](https://stackoverflow.com/tags/kentico-kontent)

| Paradigm        | Package | Downloads | Documentation |
| ------------- |:-------------:| :-------------:|  :-------------:|
| Async         | [![NuGet](https://img.shields.io/nuget/v/Kentico.Kontent.Delivery.svg)](https://www.nuget.org/packages/Kentico.Kontent.Delivery) | [![NuGet](https://img.shields.io/nuget/dt/Kentico.Kontent.delivery.svg)](https://www.nuget.org/packages/Kentico.Kontent.Delivery) | [📖](#using-the-deliveryclient) |
| Reactive      | [![NuGet](https://img.shields.io/nuget/v/Kentico.Kontent.Delivery.Rx.svg)](https://www.nuget.org/packages/Kentico.Kontent.Delivery.Rx) | [![NuGet](https://img.shields.io/nuget/dt/Kentico.Kontent.delivery.Rx.svg)](https://www.nuget.org/packages/Kentico.Kontent.Delivery.Rx) | [📖](../../wiki/Using-the-Kentico.Kontent.Delivery.Rx-reactive-library) |

## Summary

The Kentico Kontent Delivery .NET SDK is a client library used for retrieving content from Kentico Kontent.

You can use it via any of the following NuGet packages:

* [Kentico.Kontent.Delivery](https://www.nuget.org/packages/Kentico.Kontent.Delivery)
* [Kentico.Kontent.Delivery.Rx](https://www.nuget.org/packages/Kentico.Kontent.Delivery.Rx)

The first package provides the [DeliveryClient](#using-the-deliveryclient) object to consume Kentico Kontent data via the traditional async way. The second one provides the [DeliveryObservableProxy](../../wiki/Using-the-Kentico.Kontent.Delivery.Rx-reactive-library) object that enables the reactive way of consuming the data.

### Compatibility
The SDK targets the [.NET Standard 2.0](https://docs.microsoft.com/en-us/dotnet/standard/net-standard), which means it can be used in .NET Framework 4.6.1 projects and above, and .NET Core 2.0 projects and above.

## Using the DeliveryClient

The `IDeliveryClient` interface is the main interface of the SDK. Using an implementation of this interface, you can retrieve content from your Kentico Kontent projects.

We have a several extension methods on the `IServiceCollection` for configuring `DeliveryClient` services into your application.

```csharp
public void ConfigureServices(IServiceCollection services)
{
	services.AddDeliveryClient(Configuration);
}
```

By default SDK reads the configuration `DeliveryOptions` from your appsettings.json. You can also set up a `DeliveryOptions` manually by the [DeliveryOptionsBuilder](https://github.com/Kentico/kontent-delivery-sdk-net/blob/master/Kentico.Kontent.Delivery/Builders/DeliveryOptions/DeliveryOptionsBuilder.cs).


If you don't have an IoC/DI containers, use the `DeliveryClientBuilder` for manual building `IDeliveryClient`.

* `ProjectId` – sets the ID of your Kentico Kontent project. This parameter must always be set.
* `UsePreviewApi` – determines whether to use the Delivery Preview API and sets the Delivery Preview API key. See [previewing unpublished content](#previewing-unpublished-content) to learn more.
* `UseProductionApi` – determines whether to use the default production Delivery API.
* `UseSecureAccess` – determines whether authenticate requests to the production Delivery API with an API key. See [retrieving secured content](https://docs.kontent.ai/tutorials/develop-apps/get-content/securing-public-access?tech=dotnet#a-retrieving-secured-content) to learn more.
* `WaitForLoadingNewContent` – forces the client instance to wait while fetching updated content, useful when acting upon [webhook calls](https://docs.kontent.ai/tutorials/develop-apps/integrate/using-webhooks-for-automatic-updates).
* `EnableRetryPolicy` – determines whether HTTP requests will use [retry policy](#retry-capabilities). By default, the retry policy is enabled.
* `DefaultRetryPolicyOptions` – sets a [custom parameters](#retry-capabilities) for the default retry policy. By default, the SDK retries for at most 30 seconds.
* `WithCustomEndpoint` - sets a custom endpoint for the specific API (preview, production, or secured production).

```csharp
IDeliveryClient client = DeliveryClientBuilder
    .WithOptions(builder => builder
        .WithProjectId("<YOUR_PROJECT_ID>")
        .UseProductionApi()
        .WithDefaultRetryPolicyOptions(new DefaultRetryPolicyOptions {
	    DeltaBackoff = TimeSpan.FromSeconds(1),
	    MaxCumulativeWaitTime = TimeSpan.FromSeconds(10)
	})
	.Build())
    .Build();
```


### Strongly-typed responses

The `IDeliveryClient` also supports retrieving of strongly-typed models.

```csharp
// Retrieving a single content item
DeliveryItemResponse<Article> response = await client.GetItemAsync<Article>("latest_article");

// Retrieving all content items
DeliveryItemListingResponse<Article> listingResponse = await client.GetItemsAsync<Article>();
```

See [Working with Strongly Typed Models](../../wiki/Working-with-strongly-typed-models) to learn how to generate models and adjust the logic to your needs.

## Previewing unpublished content

To retrieve unpublished content, you need to create an instance of the `IDeliveryClient` with both Project ID and Preview API key. Each Kentico Kontent project has its own Preview API key.

```csharp
// Note: We recomend that you work with only either the production OR preview Delivery API within a single project.
IDeliveryClient client = DeliveryClientBuilder
    .WithOptions(builder => builder
        .WithProjectId("<YOUR_PROJECT_ID>")
        .UsePreviewApi("<YOUR_PREVIEW_API_KEY>")
        .Build())
    .Build();
```

Learn more about [previewing unpublished content using the Delivery API](https://docs.kontent.ai/tutorials/write-and-collaborate/preview-content/previewing-unpublished-content) in our Developer Hub.


## Getting content item properties

You can access information about a content item (such as its ID, codename, name, location in sitemap, date of last modification, and its content type codename) by using the `System` property.

```csharp
// Retrieves name of an article content item
articleItem.System.Name

// Retrieves codename of an article content item
articleItem.System.Codename

// Retrieves name of the content type of an article content item
articleItem.System.Type
```

## Further information

For more developer resources, visit the Kentico Kontent Developer Hub at <https://docs.kontent.ai/tutorials/develop-apps>.

## Get involved

Check out the [contributing](CONTRIBUTING.md) page to see the best places to file issues, start discussions, and begin contributing.

![Analytics](https://kentico-ga-beacon.azurewebsites.net/api/UA-69014260-4/Kentico/kontent-delivery-sdk-net?pixel)
