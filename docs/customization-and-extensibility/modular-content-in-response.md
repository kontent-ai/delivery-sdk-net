If you need to access [`modular_content` (linked content)](https://docs.kontent.ai/reference/delivery-api#tag/Linked-content-and-components/retrieve-linked-content) directly, it is possible to do so by using `Kontent.Ai.Delivery.Abstractions.ApiResponse.Content`.

```csharp
var response = await client.GetItemAsync<object>("item_codename");

var content = JObject.Parse(response.ApiResponse.Content ?? "{}");

dynamic linkedItems = content["modular_content"].DeepClone();
```

It is also possible to use [runtime type resolution](strongly-typed-models.md#adding-support-for-runtime-type-resolution) to get a strongly typed response from the dynamic response.

```csharp

// `linkedItems` used from previous code snippet
var linkedItemsTyped = (linkedItems as JObject).Values();

var itemTasks = linkedItemsTyped?.Select
(
    async source =>
    {
        // using runtime type resolution
        return await client.ModelProvider.GetContentItemModelAsync<object>(source, linkedItems);
    }
);
// A list of strongly typed models based on provided Type Provider
var items = (await Task.WhenAll(itemTasks)).ToList();
```

> If you want to see the snippets in action, check out `Kontent.Ai.Delivery.Tests.DeliveryClientTests.RetrieveContentItem_GetLinkedItems_TypeItemsManually` test.