# Deliver .NET SDK

## SUMMARY

Kentico Deliver .NET SDK is a library used for retrieving content from Kentico Deliver API from your .NET applications.

## PREREQUISITIES

In order to retrieve the content from the Kentico Deliver API, you need to have a subscription for the service at https://app.kenticocloud.com . Please read https://kenticocloud.com/docs for more information.

## BASICS

### Creating a DeliverClient instance & basic querying

**DeliverClient** of the SDK that enables you to query your content. To create instance you need to provide at least your Project ID. Here is how you can find it: https://kenticocloud.com/docs#getting-project-id
Once you have the DeliverClient instance, you can start querying your project repository by calling methods from the client class.

#### Retrieving single content item

```C#
new DeliverClient("44cb81b2-20c0-4288-81c7-ac541bf0eb48").GetItemAsync("Home");
```

#### Retrieving all content items

```C#
new DeliverClient("44cb81b2-20c0-4288-81c7-ac541bf0eb48").GetItemsAsync();
```

#### Filtering, listing and projection of your content items

The SDK supports full scale of the API querying capabilities that you can find here: http://docs.kenticodeliver.apiary.io

Here is an example of query that returns only first 10 content items of certain type with certain elements, ordered by the name:
```C#
var response = await client.GetItemsAsync(new List<IFilter> {
    new LimitFilter(10),
    new EqualsFilter("system.type", "brewer"),
    new ElementsFilter("image", "price", "product_status", "processing"),
    new Order("elements.product_name")
});
```

### Response structure

#### Single content item response

The **DeliverResponse** is a typed wrapper of the JSON response from Kentico Deliver API endpoint for retrieving single content item that is described here: http://docs.kenticodeliver.apiary.io/#reference/0/one-content-item/get-specific-content-item

* If you query a single item, you will receive a DeliverResponse instance that contains the **ContentItem** you requested and Modular content as a dynamic.

#### Single content item response

The **DeliverListingResponse** is a typed wrapper of the JSON response from Kentico Deliver API endpoint for retrieving multiple content items that is described here: http://docs.kenticodeliver.apiary.io/#reference/0/all-content-items/get-all-content-items

* If you query a multiple items, you will receive a DeliverListingResponse instance that contains:
 * **Pagination** property containing information about current page, number of items, next page URL etc.
 * List of Content Items that contains the items you requested
 * Modular content as dynamic
 
#### ContentItem structure
 
 * ContentItem then contains:
  * System with metadata such as code name, display name, type, or site map locations.
  * Elements as a dynamic object containing all the elements of your content structured by code names.
  * Methods for easier access to certain types of content elements such as modular content, or assets
  
##### Examples:
* Retrieving name of article:
```C#
ArticleItem.System.Name
```
* Retrieving a article text:
```C#
ArticleItem.Elements.text.value
```
* Retrieving a teaser image URL:
```C#
ArticleItem.GetAssets("teaser_image")[0].Url
```
* Retrieving ContentItems or related articles:
```C#
ArticleItem.GetModularContent("related_articles")
```

