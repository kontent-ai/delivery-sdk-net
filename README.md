# Deliver .NET SDK

## SUMMARY

Kentico Deliver .NET SDK is a library used for retrieving content from Kentico Deliver API.

## PREREQUISITIES

In order to retrieve the content from the Kentico Deliver API, you need to have a Deliver subscription at https://app.kenticocloud.com . Please read https://kenticocloud.com/docs for more information.

## BASICS

### Creating a DeliverClient instance & basic querying

**DeliverClient** is the main class of the SDK that enables you to query your content. To create instance you need to provide the ID of your project. More details about getting Project ID: https://kenticocloud.com/docs#getting-project-id
Once you have the DeliverClient instance, you can start querying your project repository by calling methods on the instance.

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

Here is an example of query that returns only first 10 content items of the brewer type with only the selected elements and ordered by the name:
```C#
var response = await client.GetItemsAsync(new List<IFilter> {
    new LimitFilter(10),
    new EqualsFilter("system.type", "brewer"),
    new ElementsFilter("image", "price", "product_status", "processing"),
    new Order("elements.product_name")
});
```

#### Retrieving unpublished content

To preview the unpublished content, you need to create DeliverClient instance with both Project ID and Preview API key. The Preview API key is part of your Kentico Deliver project. For more details, please read: https://kenticocloud.com/docs#previewing-unpublished-content-using-deliver-api

### Response structure

#### Single content item response

The **DeliverResponse** is a class representing the JSON response from Kentico Deliver API endpoint for retrieving single content item. Full description of the response format is described here: http://docs.kenticodeliver.apiary.io/#reference/0/one-content-item/get-specific-content-item

* If you query a single item, you will receive a DeliverResponse instance that contains the **ContentItem** you requested and Modular content in a dynamically typed property.

#### Listing response

The **DeliverListingResponse** is a class representing the JSON response from Kentico Deliver API endpoint for retrieving multiple content items. Full description of the response is described here: http://docs.kenticodeliver.apiary.io/#reference/0/all-content-items/get-all-content-items

* If you query a multiple items, you will receive a DeliverListingResponse instance that contains:
 * **Pagination** property containing information about current page, number of items, next page URL etc.
 * List of content items that contains the items you requested.
 * Modular content items in dynamically typed property.
 
#### ContentItem structure
 
 * ContentItem class provides the following properties:
  * System property with metadata such as code name, display name, type, or site map locations.
  * Elements as a dynamically typed property containing all the elements included in the response structured by code names.
  * Methods for easier access to certain types of content elements such as modular content, or assets.
  
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

