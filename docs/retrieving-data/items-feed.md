To retrieve a large number of items, for example, to warm a local cache, export content or build a static web site, the SDK provides a `IDeliveryItemsFeed` to process items in a streaming fashion. With large environments, feed has several advantages over fetching all items in a single API call:
* Processing can start as soon as the first item is received, there is no need to wait for all items.
* Memory consumption is reduced significantly.
* There is no risk of request timeouts.

To fetch data from the start of the feed, you can use

```csharp
// Process all strongly-typed content items in a streaming fashion.
IDeliveryItemsFeed<Article> feed = client.GetItemsFeed<Article>();
while(feed.HasMoreResults) 
{
    IDeliveryItemsFeedResponse<Article> response = await feed.FetchNextBatchAsync();
    foreach(Article article in response) {
        ProcessArticle(article);
    }client.GetItemsFeed
}
```

You can also pass optional parameter `continuationToken` to method `FetchNextBatchAsync`. In that case, fetch from the feed will start from the position defined in this `continuationToken` instead of its start. This parameter has the same format and functionality as `X-Continuation` header value used for [content item enumeration via Delivery REST API](https://kontent.ai/learn/reference/delivery-api/#operation/enumerate-content-items).


### Filtering and localization

Both filtering and language selection are identical to the `GetItems` method, except for `DepthParameter`, `LimitParameter`, and `SkipParameter` parameters that are not supported.

```csharp
// Process selected and projected content items in a streaming fashion.
IDeliveryItemsFeed<Brewer> feed = await client.GetItemsFeed<Brewer>(
    new LanguageParameter("es-ES"),
    new EqualsFilter("system.type", "brewer"),
    new ElementsParameter("image", "price", "product_status", "processing"),
    new OrderParameter("elements.product_name")
);
```

### Limitations

* The response does not contain linked items, only components.
* Delivery API determines how many items will be returned in a single batch.