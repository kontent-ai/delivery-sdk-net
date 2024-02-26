If you need to instantiate `IDeliveryItemResponse` or `IDeliveryItemListingResponse` objects, or your custom strongly-typed models with custom data (that is when you don't want to connect to the Delivery service directly from your unit tests), you may want to inject your own instance of `HttpClient`.

First, you need to fake the `HttpMessageHandler` class. The method of doing so is well described at [StackOverflow](https://stackoverflow.com/questions/22223223/how-to-pass-in-a-mocked-httpclient-in-a-net-test/22264503#22264503). We recommend using [RichardSzalay.MockHttp](https://www.nuget.org/packages/RichardSzalay.MockHttp/) NuGet package:

```csharp
// Arrange
var mockHttp = new MockHttpMessageHandler();
mockHttp.When("https://deliver.kontent.ai/*").Respond("application/json", "<desired json>");
```

Then you create an instance of the `HttpClient` class:

```csharp
var httpClient = mockHttp.ToHttpClient();
```

You can now use the fake `HttpClient` when creating an instance of the `IDeliveryClient` interface:

```csharp
IDeliveryClient client = DeliveryClientBuilder
    .WithEnvironmentId(Guid.NewGuid())
    .WithDeliveryHttpClient(new DeliveryHttpClient(httpClient))
    .Build();
```

See the whole example in the SDK's [HTTP client tests](../../Kontent.Ai.Delivery.Tests/FakeHttpClientTests.cs).
