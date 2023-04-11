# Universal item retrieval

> ⚠ This approach is experimental. It might contain breaking changes in the future.

The `IDeliveryClient` interface supports fetching the universal content items. Which is basically a form of representing content elements being deserialized into the dictionary of elements. And this universal item is retrieved as a part of universal response with its linked items.

> See [Strongly model retrieval approach](../customization-and-extensibility/strongly-typed-models.md) for more strictly typed model retrieval.

```csharp
// Initializes a client
IDeliveryClient deliveryClient = DeliveryClientBuilder
    .WithProjectId("975bf280-fd91-488c-994c-2f04416e5ee3")
    .Build();

// Basic retrieval
deliveryClient.GetUniversalItemAsync("article_about_coffee");

deliveryClient.GetUniversalItemsAsync();
```

This approach is beneficial if you are processing items and do not distinguish among their content types i.e. spell check, procession based only pn element level.

## Data structure

The structure is simple. It mirror the [content item object](https://kontent.ai/learn/reference/openapi/delivery-api/#section/Content-item-object). The only thing that differs that the codename of elements is used as a key to dictionarry as well as property inside the value of the element.

```csharp

// Simplified
interface IUniversalContentItem
{
    IContentItemSystemAttributes System;
    Dictionary<string, IContentElementValue> Elements;
}

}

// Simplified
interface IContentElementValue<out T> : IContentElementValue
{

    string Codename;
    string Name;
    string Type;
    T Value;
    // some element type specific properties
}
```

The response content the item(s) depending on the ednpoint used.

```csharp
// Simplified
interface IDeliveryUniversalItemResponse
{
    IUniversalContentItem Item;
    public Dictionary<string, IUniversalContentItem> LinkedItems;
}

// Simplified
interface IDeliveryUniversalItemListingResponse
{
    IList<IUniversalContentItem> Items;
    Dictionary<string, IUniversalContentItem> LinkedItems;
}
```

> It is possible to use `ModelProvider` to transform the `APIResponse` body to strongly typed model using the same approach as for [Typing modular content in response](../customization-and-extensibility/modular-content-in-response.md), but also for the item itself

## Exceptions

Using [value converters](../customization-and-extensibility/value-converters.md) and [property mapper](../customization-and-extensibility/strongly-typed-models.md#customizing-the-property-matching) is not supported fot universal items.

## Customization

You can implement your own `IUniversalModelProvider` and register it via `DeliveryClientFactory`.

```csharp

class CustomUniversalItemModelProvider : IUniversalItemModelProvider
{
    // custom implementation
    public async Task<IUniversalContentItem> GetContentItemGenericModelAsync(object item) => CustomImplementation((JObject)item);
}

var client = DeliveryClientBuilder
  .withProjectId("<PROJECT_ID>")
  .WithUniversalItemModelProvider(new CustomUniversalItemModelProvider())
  .Build();
```

## Caching

Univseral item retrieval is also supported in [caching client](../retrieving-data/caching.md).

There is the same caching implementation as for strongly typed methods `GetContentItem` and `GetContentItem`. Just as as the primary cache key `IUniversalContentItem` is used for type specification when retrieving single item and `IList<IUniversalContentItem>` for retrieving multiple items.
