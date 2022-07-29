Once you have an instance of the `IDeliveryClient`, you can start querying your project by calling methods on the instance.

```csharp
// Retrieves a single content item
IDeliveryItemResponse response = await client.GetItemAsync<About>("about_us");

// Retrieves a list of all content items
IDeliveryItemListingResponse listingResponse = await client.GetItemsAsync<About>();
```

### Filtering retrieved data

The SDK supports full scale of the API querying and [filtering capabilities](https://docs.kontent.ai/reference/delivery-api#tag/Filtering-content) as described in the Delivery API reference.

```csharp
// Retrieves a list of the specified elements from the first 10 content items of
// the 'brewer' content type, ordered by the 'product_name' element value
IDeliveryItemListingResponse<Brewer> response = await client.GetItemsAsync<Brewer>(
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
IDeliveryItemListingResponse<Brewer> response = await client.GetItemsAsync<Brewer>(
    new LanguageParameter("es-ES"),
    new ElementsParameter("image", "price", "product_status", "processing"),
    new LimitParameter(10),
    new OrderParameter("elements.product_name")
);
```

### Paging navigation

To display a paging navigation you need to retrieve the total number of items matching the search criteria. This can be achieved by adding the `IncludeTotalCountParameter` to the request parameters. With this parameter, the item listing responses will contain the total number of items in the `Pagination.TotalCount` property. This behavior can also be enabled globally by calling the `IDeliveryOptionsBuilder.IncludeTotalCount` method. Please note that response times might increase slightly.

```csharp
// Retrieves the second page of items including a total number of items matching the search criteria
IDeliveryItemListingResponse<Brewer> response = await client.GetItemsAsync<Brewer>(
    new LanguageParameter("es-ES"),
    new OrderParameter("elements.product_name"),
    new SkipParameter(5),
    new LimitParameter(5),
    new IncludeTotalCountParameter(),
```
