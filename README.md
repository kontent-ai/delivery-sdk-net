# Kentico Kontent Delivery .NET SDK

[![Build status](https://ci.appveyor.com/api/projects/status/3m3q2ads2y43bh9o/branch/master?svg=true)](https://ci.appveyor.com/project/kentico/deliver-net-sdk/branch/master)
[![CircleCI](https://circleci.com/gh/Kentico/kontent-delivery-sdk-net.svg?style=shield)](https://circleci.com/gh/Kentico/kontent-delivery-sdk-net)
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

To initialize the client, use the `DeliveryClientBuilder` class and provide a [project ID](https://docs.kontent.ai/tutorials/develop-apps/get-content/getting-content#section-getting-content-items).

```csharp
// Initializes an instance of the DeliveryClient client by building it with the DeliveryClientBuilder class
IDeliveryClient client = DeliveryClientBuilder.WithProjectId("<YOUR_PROJECT_ID>").Build();
```

You can also provide the project ID and other parameters by passing a function that returns the [`DeliveryOptions`](https://github.com/Kentico/delivery-sdk-net/blob/master/Kentico.Kontent.Delivery/Configuration/DeliveryOptions%20.cs) object to the `DeliveryClientBuilder.WithOptions` method.

We recommend creating the `DeliveryOptions` instance by using the `DeliveryOptionsBuilder` class. With the options builder, you can use the following parameters:

* `ProjectId` – sets the ID of your Kentico Kontent project. This parameter must always be set.
* `UsePreviewApi` – determines whether to use the Delivery Preview API and sets the Delivery Preview API key. See [previewing unpublished content](#previewing-unpublished-content) to learn more.
* `UseProductionApi` – determines whether to use the default production Delivery API.
* `UseSecuredProductionApi` – determines whether authenticate requests to the production Delivery API with an API key. See [retrieving secured content](https://developer.kenticokontent.com/docs/securing-public-access#section-retrieving-secured-content) to learn more.
* `WaitForLoadingNewContent` – forces the client instance to wait while fetching updated content, useful when acting upon [webhook calls](https://docs.kontent.ai/tutorials/develop-apps/integrate/using-webhooks-for-automatic-updates).
* `EnableRetryLogic` – determines whether HTTP requests will use [retry logic](#retry-capabilities). By default, the retry logic is enabled.
* `RetryPolicyOptions` – sets a [custom parameters](#retry-capabilities) for the default retry policy. By default, the SDK retries for at most 30 seconds.
* `WithCustomEndpoint` - sets a custom endpoint for the specific API (preview, production, or secured production).

```csharp
IDeliveryClient client = DeliveryClientBuilder
    .WithOptions(builder => builder
        .WithProjectId("<YOUR_PROJECT_ID>")
        .UseProductionApi
        .WithMaxRetryAttempts(maxRetryAttempts)
	.Build())
    .Build();
```

For advanced configuration options, see [using dependency injection and ASP.NET Core Configuration API](https://github.com/Kentico/delivery-sdk-net/wiki/Using-the-ASP.NET-Core-Configuration-API-and-DI-to-Instantiate-the-DeliveryClient).

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

There are special use cases in which you need to get and process a larger amount of items in a project (e.g. cache initialization, project export, static website build). This is supported in `IDeliveryClient` by using `DeliveryItemsFeed` that can iterate over items in small batches. This approach has several advantages:
* You are guaranteed to retrieve all items (as opposed to `GetItemsAsync` and paging when the project is being worked on in Kentico Kontent application)
* You can start processing items right away and use less memory, due to a limited size of each batch
* Even larger projects can be retrieved in a timely manner

```csharp
// Get items feed and iteratively process all content items in small batches.
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

There is also a strongly-typed equivalent of the feed in `IDeliveryClient` to support enumerating into a custom model.

```csharp
// Get strongly-typed items feed and iteratively fetch all content items in small batches.
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

Both filtering and language selection are very similar to `GetItems` method, except for `DepthParameter`, `LimitParameter`, and `SkipParameter` parameters. These are not supported in items feed.

```csharp
// Get a filtered feed of the specified elements of
// the 'brewer' content type, ordered by the 'product_name' element value 
DeliveryItemsFeed feed = await client.GetItemsFeed(
    new LanguageParameter("es-ES"),
    new EqualsFilter("system.type", "brewer"),
    new ElementsParameter("image", "price", "product_status", "processing"),
    new OrderParameter("elements.product_name")
);
```

### Limitations

Since this method has specific usage scenarios the response does not contain linked items, although, components are still included in the response.

Due to not supported skip and limit parameters, the size of a single batch may vary and it is not recommended to dependend on it in any way. The only guaranteed outcome is that once `HasMoreResults` property is false, you will have retrieved all the filtered items.

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

By default, the SDK uses a retry policy, asking for requested content again in case of an error. You can disable the retry policy by setting the `DeliveryOptions.EnableRetryLogic` parameter to `false`. The default policy retries the HTTP requests if the following status codes are returned:

* 408 - `RequestTimeout` 
* 429 - `TooManyRequests`
* 500 - `InternalServerError`
* 502 - `BadGateway`
* 503 - `ServiceUnavailable`
* 504 - `GatewayTimeout`

or if HTTP requests throw `HttpRequestException` with inner `WebException` and its exception status is one of the following:

* `WebExceptionStatus.ConnectFailure`
* `WebExceptionStatus.ConnectionClosed`
* `WebExceptionStatus.KeepAliveFailure`
* `WebExceptionStatus.NameResolutionFailure`
* `WebExceptionStatus.ReceiveFailure`
* `WebExceptionStatus.SendFailure`
* `WebExceptionStatus.Timeout`

There are two parameters in `DeliveryOptions.RetryPolicyOptions` to customize the default retry policy: `DeltaBackoff` and `MaxCumulativeWaitTime`. 

The consecutive retry attempts are delayed exponentically where the delay (wait time) is based on the `DeltaBackoff` and the number of attemted retries. For status codes 429 (`TooManyRequests`) and 503 (`ServiceUnavailable`) the wait time is read from a standard `Retry-After` header if present in the response. Should the wait time exceed `MaxCumulativeWaitTime` no further retry is attempted and the policy throws either `DeliveryException`, if response resulted in one of the retried status codes, or the original retried `HttpRequestException` thrown by the request. 

The defaults for `RetryPolicyOptions` are `DeltaBackoff` of 1 second and `MaxCumulativeWaitTime` of 30 seconds. Custom values can be set through `DeliveryClientBuilder`.

The default retry policy is implemented without any 3rd party dependency, but you can create your custom policy, for example using [Polly](https://github.com/App-vNext/Polly), by implementing `IRetryPolicy` and `IRetryPolicyProvider`. The instance of the provider can be set to `IDeliveryClient` implementation through the `DeliveryClientBuilder` class or by registering it to the `ServiceCollection`.

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
