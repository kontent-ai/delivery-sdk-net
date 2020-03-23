# Kentico Kontent Delivery .NET SDK

[![Build status](https://ci.appveyor.com/api/projects/status/3m3q2ads2y43bh9o/branch/master?svg=true)](https://ci.appveyor.com/project/kentico/deliver-net-sdk/branch/master)
[![CircleCI](https://circleci.com/gh/Kentico/kontent-delivery-sdk-net.svg?style=shield)](https://circleci.com/gh/Kentico/kontent-delivery-sdk-net)
[![codecov](https://codecov.io/gh/Kentico/kontent-delivery-sdk-net/branch/master/graph/badge.svg)](https://codecov.io/gh/Kentico/kontent-delivery-sdk-net)
[![Stack Overflow](https://img.shields.io/badge/Stack%20Overflow-ASK%20NOW-FE7A16.svg?logo=stackoverflow&logoColor=white)](https://stackoverflow.com/tags/kentico-kontent)

| Paradigm        | Package  | Downloads | Documentation |
| ------------- |:-------------:| :-------------:|  :-------------:|
| Async         | [![NuGet](https://img.shields.io/nuget/v/Kentico.Kontent.Delivery.svg)](https://www.nuget.org/packages/Kentico.Kontent.Delivery) | [![NuGet](https://img.shields.io/nuget/dt/Kentico.Kontent.delivery.svg)](https://www.nuget.org/packages/Kentico.Kontent.Delivery) | [📖](#using-the-deliveryclient) |
| Reactive      | [![NuGet](https://img.shields.io/nuget/v/Kentico.Kontent.Delivery.Rx.svg)](https://www.nuget.org/packages/Kentico.Kontent.Delivery.Rx) | [![NuGet](https://img.shields.io/nuget/dt/Kentico.Kontent.delivery.Rx.svg)](https://www.nuget.org/packages/Kentico.Kontent.Delivery.Rx) | [📖](#using-the-kenticokontentdeliveryrx-reactive-library) |

## Summary

The Kentico Kontent Delivery .NET SDK is a client library used for retrieving content from Kentico Kontent.

You can use it via any of the following NuGet packages:

* [Kentico.Kontent.Delivery](https://www.nuget.org/packages/Kentico.Kontent.Delivery)
* [Kentico.Kontent.Delivery.Rx](https://www.nuget.org/packages/Kentico.Kontent.Delivery.Rx)

The first package provides the [DeliveryClient](#using-the-deliveryclient) object to consume Kentico Kontent data via the traditional async way. The second one provides the [DeliveryObservableProxy](#using-the-kenticokontentdeliveryrx-reactive-library) object that enables the reactive way of consuming the data.

The SDK targets the [.NET Standard 2.0](https://docs.microsoft.com/en-us/dotnet/standard/net-standard), which means it can be used in .NET Framework 4.6.1 projects and above, and .NET Core 2.0 projects and above.

## Using the DeliveryClient

The `IDeliveryClient` interface is the main interface of the SDK. Using an implementation of this interface, you can retrieve content from your Kentico Kontent projects.

We have a several extension methods on the `IServiceCollection` for configuring `DeliveryClient` services into your application.

```csharp
services.AddDeliveryClient(Configuration);
```


By default SDK reads the configuration `DeliveryOptions` from yor appsettings.json. You can also set up a `DeliveryOptions` manually by the [DeliveryOptionsBuilder](https://github.com/Kentico/kontent-delivery-sdk-net/blob/master/Kentico.Kontent.Delivery/Builders/DeliveryOptions/DeliveryOptionsBuilder.cs).


If you need to use more configuration for your `IDeliveryClient` you can register named `DeliveryClient`.

```csharp
services.AddDeliveryClient("production", Configuration, "DeliveryOptions1");
services.AddDeliveryClient("preview", Configuration, "DeliveryOptions2");
```

For resolving named client use the [IDeliveryClientFactory](https://github.com/Kentico/kontent-delivery-sdk-net/blob/master/Kentico.Kontent.Delivery.Abstractions/IDeliveryClientFactory.cs), which is registeted in your DI container.
```csharp
public HomeController(IDeliveryClientFactory deliveryClientFactory)
{
    var deliveryClient = deliveryClientFactory.Get("production");
}
```


If you want to use the `HttpClientFactory` for resolving a `HttpClient`, then just register our `DeliveryHttpClient` into `AddHttpClient` pipeline.

```csharp
services.AddHttpClient<IDeliveryHttpClient, DeliveryHttpClient>();
```

We also provide a package for memory caching, which is fully compatible with our SDK - [Kentico.Kontent.Delivery.Caching](https://www.nuget.org/packages/Kentico.Kontent.Delivery.Caching).

Register cache service for the named client with name `preview`. You can also implement your custom implementation by this interface -  [IDeliveryCacheManager](https://github.com/Kentico/kontent-delivery-sdk-net/blob/master/Kentico.Kontent.Delivery.Abstractions/IDeliveryCacheManager.cs).
```csharp
services.AddDeliveryClientCache("client1", new DeliveryCacheOptions());
```



If you don't have a IoC/DI containers, use the `DeliveryClientBuilder` for manual building `IDeliveryClient`.
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

## Basic querying

Once you have an instance of the `IDeliveryClient`, you can start querying your project by calling methods on the instance.

```csharp
// Retrieves a single content item
DeliveryItemResponse response = await client.GetItemAsync("about_us");

// Retrieves a list of all content items
DeliveryItemListingResponse listingResponse = await client.GetItemsAsync();
```

### Filtering retrieved data

The SDK supports full scale of the API querying and filtering capabilities as described in the [API reference](https://developer.kenticocloud.com/reference#content-filtering).

```csharp
// Retrieves a list of the specified elements from the first 10 content items of
// the 'brewer' content type, ordered by the 'product_name' element value
DeliveryItemListingResponse response = await client.GetItemsAsync(
    new EqualsFilter("system.type", "brewer"),
    new ElementsParameter("image", "price", "product_status", "processing"),
    new LimitParameter(10),
    new OrderParameter("elements.product_name")
);
```

### Getting localized items

The language selection is just a matter of specifying the `LanguageParameter` parameter with a codename of the required language.

```csharp
// Retrieves a list of the specified elements from the first 10 content items of
// the 'brewer' content type, ordered by the 'product_name' element value
DeliveryItemListingResponse response = await client.GetItemsAsync(
    new LanguageParameter("es-ES"),
    new EqualsFilter("system.type", "brewer"),
    new ElementsParameter("image", "price", "product_status", "processing"),
    new LimitParameter(10),
    new OrderParameter("elements.product_name")
);
```

### Paging navigation

To display a paging navigation you need to retrieve the total number of items matching the search criteria. This can be achieved by adding the `IncludeTotalCountParameter` to the request parameters. With this parameter the item listing responses will contain the total number of items in the `Pagination.TotalCount` property. This behavior can also be enabled globally by calling the `IDeliveryOptionsBuilder.IncludeTotalCount` method. Please note that response times might increase slightly.

```csharp
// Retrieves the second page of items including total number of items matching the search criteria
DeliveryItemListingResponse response = await client.GetItemsAsync(
    new LanguageParameter("es-ES"),
    new EqualsFilter("system.type", "brewer"),
    new OrderParameter("elements.product_name"),
    new SkipParameter(5),
    new LimitParameter(5),
    new IncludeTotalCountParameter(),
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

## Enumerating all items

To retrieve a large amount of items, for example to warm a local cache, export content or build a static web site, the SDK provides a `DeliveryItemsFeed` to process items in a streaming fashion. With large projects feed has several advantages over fetching all items in a single API call:
* Processing can start as soon as the first item is received, there is no need to wait for all items.
* Memory consumption is reduced significantly.
* There is no risk of request timeouts.

```csharp
// Process all content items in a streaming fashion.
DeliveryItemsFeed feed = client.GetItemsFeed();
while(feed.HasMoreResults) 
{
    DeliveryItemsFeedResponse response = await feed.FetchNextBatchAsync();
    foreach(ContentItem item in response) {
        ProcessItem(item);
    }
}
```

### Strongly-typed models

There is also a strongly-typed equivalent of the items feed.

```csharp
// Process all strongly-typed content items in a streaming fashion.
DeliveryItemsFeed<Article> feed = client.GetItemsFeed<Article>();
while(feed.HasMoreResults) 
{
    DeliveryItemsFeedResponse<Article> response = await feed.FetchNextBatchAsync();
    foreach(Article article in response) {
        ProcessArticle(article);
    }
}
```

### Filtering and localization

Both filtering and language selection are identical to the `GetItems` method, except for `DepthParameter`, `LimitParameter`, and `SkipParameter` parameters that are not supported.

```csharp
// Process selected and projected content items in a streaming fashion.
DeliveryItemsFeed feed = await client.GetItemsFeed(
    new LanguageParameter("es-ES"),
    new EqualsFilter("system.type", "brewer"),
    new ElementsParameter("image", "price", "product_status", "processing"),
    new OrderParameter("elements.product_name")
);
```

### Limitations

* The response does not contain linked items, only components.
* Delivery API determines how many items will be returned in a single batch.

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

## Response structure

For full description of single and multiple content item JSON response formats, see our [API reference](https://developer.kenticocloud.com/reference#response-structure).

### Single content item response

When retrieving a single content item, you get an instance of the `DeliveryItemResponse` class. This class represents the JSON response from the Delivery API endpoint and contains the requested `ContentItem` as a property.

### Multiple content items response

When retrieving a list of content items, you get an instance of the `DeliveryItemListingResponse`. This class represents the JSON response from the Delivery API endpoint and contains:

* `Pagination` property with information about the following:
  * `Skip`: requested number of content items to skip
  * `Limit`: requested page size
  * `Count`: the total number of retrieved content items
  * `NextPageUrl`: the URL of the next page
* A list of the requested content items

### Content items feed response

When retrieving an items feed, you get an instance of the `DeliverItemsFeedResponse`. This class represents the JSON response from the Delivery API endpoint and contains a list of requested content items.

### ContentItem structure

The `ContentItem` class provides the following:

* `System` property with metadata such as code name, display name, type, or sitemap location.
* `Elements` as a dynamically typed property containing all the elements included in the response structured by code names.
* Methods for easier access to certain types of content elements such as linked items, or assets.

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

## Getting element values

The SDK provides methods for retrieving content from content elements such as Asset, Text, Rich Text, Multiple choice, etc.

### Text

For Text elements, you can use the `GetString` method.

```csharp
// Retrieves an article text from the 'body_copy' Text element
articleItem.GetString("body_copy")
```

### Rich text

* The Rich text element can contain links to other content items within your project. See [Resolving links to content items](https://github.com/Kentico/delivery-sdk-net/wiki/Resolving-Links-to-Content-Items) for more details.
* The Rich text element can contain components and other content items. See [Structured Rich text rendering](https://github.com/Kentico/delivery-sdk-net/wiki/Structured-Rich-text-rendering) for more details. To learn more about components and linked content, visit our [API Reference](https://developer.kenticocloud.com/v1/reference#linked-content).

### Asset

```csharp
// Retrieves a teaser image URL
articleItem.GetAssets("teaser_image").First().Url
```

### Multiple choice

To get a list of options defined in a Multiple choice content element, you first need to retrieve the content element itself. For this purpose, you can use the `GetContentElementAsync` method, which takes the codename of a content type and the codename of a content element.

```csharp
// Retrieves the 'processing' element of the 'coffee' content type
ContentElement element = await client.GetContentElementAsync("coffee", "processing");
```

After you retrieve the Multiple choice element, you can work with its list of options. Each option has the following properties:


Property | Description | Example
---------|----------|---------
 Name | The display name of the option. | `Dry (Natural)`
 Codename | The codename of the option. | `dry__natural_`

To put the element's options in a list, you can use the following code:

```csharp
List<SelectListItem> items = new List<SelectListItem>();

foreach (var option in element.Options)
{
    items.Add(new SelectListItem {
        Text = option.Name,
        Value = option.Codename,
        Selected = (option.Codename == "semi_dry")
    });
}
```

### Linked items

```csharp
// Retrieves related articles
articleItem.GetLinkedItems("related_articles")
```

If items feed is used to retrieve content items, only components can be retrieved by this method.

## Using the Image transformations

The [ImageUrlBuilder class](https://github.com/Kentico/delivery-sdk-net/blob/master/Kentico.Kontent.Delivery/ImageTransformation/ImageUrlBuilder.cs) exposes methods for applying image transformations on the Asset URL.

```csharp
string assetUrl = articleItem.GetAssets("teaser_image").First().Url;
ImageUrlBuilder builder = new ImageUrlBuilder(assetUrl);
string transformedAssetUrl = builder.WithFocalPointCrop(560, 515, 2)
                                    .WithDPR(3)
                                    .WithAutomaticFormat(ImageFormat.Png)
                                    .WithCompression(ImageCompression.Lossy)
                                    .WithQuality(85)
                                    .Url;
```

For list of supported transformations and more information visit the Kentico Delivery API reference at <https://developer.kenticocloud.com/v1/reference?#image-transformation>.

## Retry capabilities

By default, the SDK uses a retry policy, asking for requested content again in case of an error. You can disable the retry policy by setting the `DeliveryOptions.EnableRetryPolicy` parameter to `false`. The default policy retries the HTTP requests if the following status codes are returned:

* 408 - `RequestTimeout` 
* 429 - `TooManyRequests`
* 500 - `InternalServerError`
* 502 - `BadGateway`
* 503 - `ServiceUnavailable`
* 504 - `GatewayTimeout`

or if there is one of the following connection problems:

* `ConnectFailure`
* `ConnectionClosed`
* `KeepAliveFailure`
* `NameResolutionFailure`
* `ReceiveFailure`
* `SendFailure`
* `Timeout`

The default retry policy performs retries using a randomized exponential back off scheme to determine the interval between retries. It can be customized by changing parameters in `DeliveryOptions.RetryPolicyOptions`. The `DeltaBackoff` parameter specifies the back-off interval between retries. The `MaxCumulativeWaitTime` parameter specifies the maximum cumulative wait time. If the cumulative wait time exceeds this value, the client will stop retrying and return the error to the application. The default retry policy also respects the `Retry-After` response header.

You can create your custom retry policy, for example with [Polly](https://github.com/App-vNext/Polly), by implementing `IRetryPolicy` and `IRetryPolicyProvider` interfaces. The custom retry policy provider can be registered with `DeliveryClientBuilder.WithRetryPolicyProvider` or with the `ServiceCollection`.

## Using the Kentico.Kontent.Delivery.Rx reactive library

The [DeliveryObservableProxy class](https://github.com/Kentico/delivery-sdk-net/blob/master/Kentico.Kontent.Delivery.Rx/DeliveryObservableProxy.cs) provides a reactive way of retrieving Kentico Kontent content.

The `DeliveryObservableProxy` class constructor accepts an [IDeliveryClient](https://github.com/Kentico/delivery-sdk-net/blob/master/Kentico.Kontent.Delivery/IDeliveryClient.cs) instance, therefore you are free to create the `IDeliveryClient` implementation (or its derivatives) in any of [the supported ways](#using-the-deliveryclient).

```csharp
public IDeliveryClient DeliveryClient => DeliveryClientBuilder.WithProjectId("975bf280-fd91-488c-994c-2f04416e5ee3").Build();
public DeliveryObservableProxy DeliveryObservableProxy => new DeliveryObservableProxy(DeliveryClient);
```

The `DeliveryObservableProxy` class exposes methods that mirror the public methods of the [IDeliveryClient](https://github.com/Kentico/delivery-sdk-net/blob/master/Kentico.Kontent.Delivery/IDeliveryClient.cs). The methods have the same names, with an `Observable` suffix. They call the `IDeliveryClient` methods in the background.

```csharp
IObservable<Article> articlesWithBaristaPersona =
	DeliveryObservableProxy.GetItemsObservable<Article>(new ContainsFilter("elements.personas", "barista"));
```

Unlike most of the `IDeliveryClient` methods that return data wrapped in `Delivery*Response` objects, their `*Observable` counterparts always return sequences of the Kentico Kontent artifacts themselves (not wrapped). Should an error response be returned by the `IDeliveryClient` implementation, the observable sequence will terminate with the conventional [OnError](https://docs.microsoft.com/en-us/dotnet/api/system.iobserver-1.onerror) call.

## Using [SourceLink](https://github.com/dotnet/sourcelink/) for debugging

This repository is configured to generate a SourceLink tag in the NuGet package that allows debugging this repository's source code when it is referenced as a Nuget package. The source code is downloaded directly from GitHub to Visual Studio.

### How to configure SourceLink

1. Open a solution with a project referencing the Kentico.Kontent.Delivery (or Kentico.Kontent.Delivery.RX) Nuget package.
2. Open Tools -> Options -> Debugging -> General.
    * Clear **Enable Just My Code**.
    * Select **Enable Source Link Support**.
    * (Optional) Clear **Require source files to exactly match the original version**.
3. Build your solution.
4. [Add a symbol server `https://symbols.nuget.org/download/symbols`](https://blog.nuget.org/20181116/Improved-debugging-experience-with-the-NuGet-org-symbol-server-and-snupkg.html)
  * ![Add a symbol server in VS](/.github/assets/vs-nuget-symbol-server.PNG)
5. Run a debugging session and try to step into the Kentico.Kontent.Delivery code.
6. Allow Visual Studio to download the source code from GitHub.
  * ![SourceLink confirmation dialog](/.github/assets/allow_sourcelink_download.png)

**Now you are able to debug the source code of our library without needing to download the source code manually!**


## Further information

For more developer resources, visit the Kentico Kontent Developer Hub at <https://docs.kontent.ai/tutorials/develop-apps>.

### Building the sources

Prerequisites:

**Required:**
[.NET Core SDK](https://www.microsoft.com/net/download/core).

Optional:
* [Visual Studio 2017](https://www.visualstudio.com/vs/) for full experience
* or [Visual Studio Code](https://code.visualstudio.com/)

## Feedback & Contributing

Check out the [contributing](https://github.com/Kentico/delivery-sdk-net/blob/master/CONTRIBUTING.md) page to see the best places to file issues, start discussions, and begin contributing.

### Wall of Fame
We would like to express our thanks to the following people who contributed and made the project possible:

- [Jarosław Jarnot](https://github.com/jjarnot-vimanet) - [Vimanet](http://vimanet.com)
- [Varinder Singh](https://github.com/VarinderS) - [Kudos Web](http://www.kudosweb.com)
- [Charith Sooriyaarachchi](https://github.com/charithsoori) - [99X Technology](http://www.99xtechnology.com/)

Would you like to become a hero too? Pick an [issue](https://github.com/Kentico/delivery-sdk-net/issues) and send us a pull request!

![Analytics](https://kentico-ga-beacon.azurewebsites.net/api/UA-69014260-4/Kentico/delivery-sdk-net?pixel)
