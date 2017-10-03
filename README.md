# Kentico Cloud Delivery .NET SDK

[![Build status](https://ci.appveyor.com/api/projects/status/3m3q2ads2y43bh9o/branch/master?svg=true)](https://ci.appveyor.com/project/kentico/deliver-net-sdk/branch/master)
[![NuGet](https://img.shields.io/nuget/v/KenticoCloud.Delivery.svg)](https://www.nuget.org/packages/KenticoCloud.Delivery)
[![NuGet](https://img.shields.io/nuget/dt/kenticocloud.delivery.svg)](https://www.nuget.org/packages/KenticoCloud.Delivery)
[![Forums](https://img.shields.io/badge/chat-on%20forums-orange.svg)](https://forums.kenticocloud.com)

## Summary

The Kentico Cloud Delivery .NET SDK is a client library used for retrieving content from Kentico Cloud. You can use the SDK in the form of a [NuGet package](https://www.nuget.org/packages/KenticoCloud.Delivery).

## Prerequisites

To retrieve content from a Kentico Cloud project via the Delivery API, you first need to activate the API for the project. See our documentation on how you can [activate the Delivery API](https://developer.kenticocloud.com/docs/using-delivery-api#section-enabling-the-delivery-api-for-your-projects).

## Using the DeliveryClient

The `DeliveryClient` class is the main class of the SDK. Using this class, you can retrieve content from your Kentico Cloud projects.

To create an instance of the class, you need to provide a [project ID](https://developer.kenticocloud.com/docs/using-delivery-api#section-getting-project-id).

```csharp
// Initializes an instance of the DeliveryClient client
DeliveryClient client = new DeliveryClient("975bf280-fd91-488c-994c-2f04416e5ee3");
```

You can also provide the project ID and other parameters by passing the [`DeliveryOptions`](https://github.com/Kentico/delivery-sdk-net/blob/master/KenticoCloud.Delivery/Configuration/DeliveryOptions%20.cs) object to the class constructor. The `DeliveryOptions` object can be used to set the following parameters:

* `PreviewApiKey` – sets the Delivery Preview API key.
* `ProjectId` – sets the project identifier.
* `UsePreviewApi` – determines whether to use the Delivery Preview API.
* `WaitForLoadingNewContent` – makes the client instance wait while fetching updated content, useful when acting upon [webhook calls](https://developer.kenticocloud.com/docs/webhooks#section-requesting-new-content).

For advanced configuration options using Dependency Injection and ASP.NET Core Configuration API, see the SDK's [wiki](https://github.com/Kentico/delivery-sdk-net/wiki/Using-the-ASP.NET-Core-Configuration-API-and-DI-to-Instantiate-the-DeliveryClient).

Once you create a `DeliveryClient`, you can start querying your project repository by calling methods on the client instance. See [Basic querying](#basic-querying) for details.

### Filtering retrieved data

The SDK supports full scale of the API querying and filtering capabilities as described in the [API reference](https://developer.kenticocloud.com/reference#filtering-content-items).

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

### Previewing unpublished content

To retrieve unpublished content, you need to create a `DeliveryClient` with both Project ID and Preview API key. Each Kentico Cloud project has its own Preview API key. 

```csharp
// Note: Within a single project, we recommend that you work with only
// either the production or preview Delivery API, not both.
DeliveryClient client = new DeliveryClient("YOUR_PROJECT_ID", "YOUR_PREVIEW_API_KEY");
```

For more details, see [Previewing unpublished content using the Delivery API](https://developer.kenticocloud.com/docs/preview-content-via-api).

## Basic querying

Once you have a `DeliveryClient` instance, you can start querying your project repository by calling methods on the instance.

```csharp
// Retrieves a single content item
DeliveryItemResponse response = await client.GetItemAsync("about_us");

// Retrieves a list of all content items
DeliveryItemListingResponse listingResponse = await client.GetItemsAsync();
```

### Strongly-typed responses

The `DeliveryClient` also supports retrieving of strongly-typed models.

```csharp
// Retrieving a single content item
DeliveryItemResponse response = await client.GetItemAsync<Article>("latest_article");

// Retrieving all content items
DeliveryItemListingResponse listingResponse = await client.GetItemsAsync<Article>();
```

See [Working with Strongly Typed Models](https://github.com/Kentico/delivery-sdk-net/wiki/Working-with-Strongly-Typed-Models-(aka-Code-First-Approach)) in the wiki to learn how to generate models and adjust the logic to your needs.

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

### ContentItem structure

The `ContentItem` class provides the following:

* `System` property with metadata such as code name, display name, type, or sitemap location.
* `Elements` as a dynamically typed property containing all the elements included in the response structured by code names.
* Methods for easier access to certain types of content elements such as modular content, or assets.

## Getting content item properties

You can access information about a content item (i.e., its ID, codename, name, location in sitemap, date of last modification, and its content type codename) by using the `System` property.

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

### Text and Rich text

For text elements, you can use the `GetString` method.

```csharp
// Retrieves an article text from the 'body_copy' Text element
articleItem.GetString("body_copy")
```

The Rich text element can contain links to other content items within your project. See [Resolving links to content items](https://github.com/Kentico/delivery-sdk-net/wiki/Resolving-Links-to-Content-Items) for more details.

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

### Modular content

```csharp
// Retrieves related articles
articleItem.GetModularContent("related_articles")
```

## Further information

For more developer resources, visit the Kentico Cloud Developer Hub at <https://developer.kenticocloud.com>.

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
