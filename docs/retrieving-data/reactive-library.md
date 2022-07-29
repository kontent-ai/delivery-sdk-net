The [DeliveryObservableProxy class](https://github.com/kontent-ai/delivery-sdk-net/Kontent.Ai.Delivery.Rx/DeliveryObservableProxy.cs) provides a reactive way of retrieving content from Kontent.

The `DeliveryObservableProxy` class constructor accepts an [IDeliveryClient](https://github.com/kontent-ai/delivery-sdk-net/Kontent.Ai.Delivery/IDeliveryClient.cs) instance, therefore you are free to create the `IDeliveryClient` implementation (or its derivatives) in any of the supported ways.

```csharp
public IDeliveryClient DeliveryClient => DeliveryClientBuilder.WithProjectId("975bf280-fd91-488c-994c-2f04416e5ee3").Build();
public DeliveryObservableProxy DeliveryObservableProxy => new DeliveryObservableProxy(DeliveryClient);
```

The `DeliveryObservableProxy` class exposes methods that mirror the public methods of the [IDeliveryClient](https://github.com/kontent-ai/delivery-sdk-net/Kontent.Ai.Delivery/IDeliveryClient.cs). The methods have the same names, with an `Observable` suffix. They call the `IDeliveryClient` methods in the background.

```csharp
IObservable<Article> articlesWithBaristaPersona =
	DeliveryObservableProxy.GetItemsObservable<Article>(new ContainsFilter("elements.personas", "barista"));
```

Unlike most of the `IDeliveryClient` methods that return data wrapped in `IDelivery*Response` objects, their `*Observable` counterparts always return sequences of the Kontent.ai artifacts themselves (not wrapped). Should an error response be returned by the `IDeliveryClient` implementation, the observable sequence will terminate with the conventional [OnError](https://docs.microsoft.com/en-us/dotnet/api/system.iobserver-1.onerror) call.