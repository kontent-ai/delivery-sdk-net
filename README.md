# Kentico Cloud Delivery .NET SDK

[![Build status](https://ci.appveyor.com/api/projects/status/3m3q2ads2y43bh9o/branch/master?svg=true)](https://ci.appveyor.com/project/kentico/deliver-net-sdk/branch/master)
[![NuGet](https://img.shields.io/nuget/v/KenticoCloud.Delivery.svg)](https://www.nuget.org/packages/KenticoCloud.Delivery)

## Summary

The Kentico Cloud Delivery .NET SDK is a client library used for retrieving content from Kentico Cloud. You can use the SDK in the form of a [NuGet package](https://www.nuget.org/packages/KenticoCloud.Delivery).

## Prerequisites

To retrieve content from a Kentico Cloud project via the Delivery API, you first need to activate the Delivery API for the project. For more information see our [documentation](https://developer.kenticocloud.com/docs/using-delivery-api#section-enabling-the-delivery-api-for-your-projects).

## Basic scenarios

### Creating a DeliveryClient instance & basic querying

The `DeliveryClient` class is the main class of the SDK that enables you to retrieve content from a project. To create an instance of the class, you need to provide the ID of your project. See the [documentation](https://developer.kenticocloud.com/docs/using-delivery-api#section-getting-project-id) on how to get the project ID.
Once you have a `DeliveryClient` instance, you can start querying your project repository by calling methods on the instance.

#### Retrieving single content item

```C#
new DeliveryClient("975bf280-fd91-488c-994c-2f04416e5ee3").GetItemAsync("about_us");
```

#### Retrieving all content items

```C#
new DeliveryClient("975bf280-fd91-488c-994c-2f04416e5ee3").GetItemsAsync();
```

#### Filtering, listing and projection of your content items

The SDK supports full scale of the API querying capabilities as described in the [API reference](https://developer.kenticocloud.com/reference).

Here is an example of a query that returns the selected elements from the first 10 content items of the `brewer` content type and ordered by the `product_name` element's value:

```C#
var response = await client.GetItemsAsync(
    new EqualsFilter("system.type", "brewer"),
    new ElementsParameter("image", "price", "product_status", "processing"),
    new LimitParameter(10),
    new OrderParameter("elements.product_name")
);
```

#### Retrieving unpublished content

To preview unpublished content, you need to create a `DeliveryClient` instance with both Project ID and Preview API key. The Preview API key is part of your Kentico Cloud project. For more details, see [Previewing unpublished content using the Delivery API](https://developer.kenticocloud.com/docs/preview-content-via-api).

### Response structure

#### Single content item response

The `DeliveryItemResponse` is a class representing the JSON response from the Delivery API endpoint for retrieving a single content item. You can find the full description of the single content item response format in the [API reference](https://developer.kenticocloud.com/reference#view-a-content-item).

* If you query a single item, you will receive a `DeliveryItemResponse` instance that contains the **ContentItem** you requested and Modular content in a dynamically typed property.

#### Listing response

The `DeliveryItemListingResponse` is a class representing the JSON response from the Delivery API endpoint for retrieving multiple content items. You can find the full description of the multiple content item response format in the [API reference](https://developer.kenticocloud.com/reference#list-content-items).

* If you query multiple content items, you will receive a `DeliveryItemListingResponse` instance that contains:
  * **Pagination** property with information about the current page, total number of retrieved content items, next page URL, etc.
  * A list of the requested content items.
  * Modular content items in a dynamically typed property.

#### ContentItem structure

* The `ContentItem` class provides the following properties:
  * `system` property with metadata such as code name, display name, type, or sitemap location.
  * `elements` as a dynamically typed property containing all the elements included in the response structured by code names.
  * Methods for easier access to certain types of content elements such as modular content, or assets.

##### Examples

* Retrieving the name of an article content item:

```C#
articleItem.System.Name
```

* Retrieving an article text:

```C#
articleItem.GetString("body_copy")
```

* Retrieving a teaser image URL:

```C#
articleItem.GetAssets("teaser_image").First().Url
```

* Retrieving related articles:

```C#
articleItem.GetModularContent("related_articles")
```