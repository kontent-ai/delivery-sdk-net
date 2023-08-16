# Kontent.ai Delivery .NET SDK

[![Build & Test](https://github.com/kontent-ai/delivery-sdk-net/actions/workflows/integrate.yml/badge.svg)](https://github.com/kontent-ai/delivery-sdk-net/actions/workflows/integrate.yml)
[![codecov](https://codecov.io/gh/kontent-ai/delivery-sdk-net/branch/master/graph/badge.svg)](https://app.codecov.io/gh/kontent-ai/delivery-sdk-net)
[![Stack Overflow](https://img.shields.io/badge/Stack%20Overflow-ASK%20NOW-FE7A16.svg?logo=stackoverflow&logoColor=white)](https://stackoverflow.com/tags/kontent-ai)
[![Discord](https://img.shields.io/discord/821885171984891914?color=%237289DA&label=Kontent.ai%20Discord&logo=discord)](https://discord.gg/SKCxwPtevJ)

| Paradigm |                                                                  Package                                                                  |                                                                Downloads                                                                |                                                                  Compatibility                                                                   |                                Documentation                                 |
| -------- | :---------------------------------------------------------------------------------------------------------------------------------------: | :-------------------------------------------------------------------------------------------------------------------------------------: | :----------------------------------------------------------------------------------------------------------------------------------------------: | :--------------------------------------------------------------------------: |
| Async    |    [![NuGet](https://img.shields.io/nuget/v/Kontent.Ai.Delivery.svg)](https://www.nuget.org/packages/Kontent.Ai.Delivery)    |    [![NuGet](https://img.shields.io/nuget/dt/Kontent.Ai.Delivery.svg)](https://www.nuget.org/packages/Kontent.Ai.Delivery)    | [`net6.0`](https://dotnet.microsoft.com/download/dotnet/6.0) |                            [📖 Wiki](./docs)                             |
| Reactive | [![NuGet](https://img.shields.io/nuget/v/Kontent.Ai.Delivery.Rx.svg)](https://www.nuget.org/packages/Kontent.Ai.Delivery.Rx) | [![NuGet](https://img.shields.io/nuget/dt/Kontent.Ai.Delivery.Rx.svg)](https://www.nuget.org/packages/Kontent.Ai.Delivery.Rx) | [`net6.0`](https://dotnet.microsoft.com/download/dotnet/6.0) | [📖 Wiki](./docs/retrieving-data/reactive-library.md) |

## Summary

The Kontent.ai Delivery .NET SDK is a client library that lets you easily retrieve content from [Kontent.ai](https://kontent.ai).

### Getting started

Installation via Package Manager Console in Visual Studio:

```powershell
PM> Install-Package Kontent.Ai.Delivery
```

Installation via .NET CLI:

```console
> dotnet add <TARGET PROJECT> package Kontent.Ai.Delivery
```

## Usage

To retrieve content from your Kontent.ai projects, you'll be using an implementation of the `IDeliveryClient` interface. This is the main interface of the SDK. Here's how you can instantiate and use the Delivery client either [with DI/IoC](#use-dependency-injection-ideal-for-aspnet-core-web-apps "Usage with dependency injection") or [without DI/IoC](#usage-without-iocdi-containers-ideal-for-console-apps-unit-tests "Usage without dependency injection").

### Use dependency injection (ideal for ASP.NET Core web apps)

**Startup.cs**

```csharp
public void ConfigureServices(IServiceCollection services)
{
	services.AddDeliveryClient(Configuration);
}
```

**HomeController.cs**

```csharp
public class HomeController
{
	private IDeliveryClient _client;

	public HomeController(IDeliveryClient deliveryClient)
	{
		_client = deliveryClient;
	}
}
```

In this case, the SDK reads the configuration from the `DeliveryOptions` section of the `Configuration` object. There are many ways of providing the configuration to the `DeliveryClient` as well as many advanced registration scenarios which you can all find in the [Wiki](./docs/configuration/dependency-injection.md).

To see a complete working example, go to one of our sample apps:

- [Kontent.ai ASP.NET Core MVC](https://github.com/kontent-ai/sample-app-net) or
- [Kontent.ai ASP.NET Core Razor Pages](https://github.com/kontent-ai/sample-app-razorpages)

To spin up a fully configured blank site quickly, use the:

- [Kontent.ai ASP.NET Core MVC boilerplate](https://github.com/kontent-ai/boilerplate-net)

### Usage without IoC/DI containers (ideal for console apps, unit tests...)

You can also set up a `DeliveryOptions` manually using the [`DeliveryClientBuilder`](https://github.com/kontent-ai/delivery-sdk-net/blob/master/Kontent.Ai.Delivery/Builders/DeliveryClient/DeliveryClientBuilder.cs).

```csharp
IDeliveryClient _client = DeliveryClientBuilder
    .WithOptions(builder => builder
        .WithProjectId("<YOUR_PROJECT_ID>")
	.UseProductionApi()
	.Build())
    .Build();
```

### Your first request

Use the [.NET code generator](https://github.com/kontent-ai/model-generator-net) to generate POCO models:

```csharp
public class Article
{
        public string Title { get; set; }
        public string Summary { get; set; }
	public string Body { get; set; }
	public DateTime? PostDate { get; set; }
	public ContentItemSystemAttributes System { get; set; }
}
```

Call the `IDeliveryClient` to retrieve data from Kontent:

```csharp
// Retrieving a single content item
var response = await _client.GetItemAsync<Article>("<article_codename>");
var title = response.Item.Title; // -> "On Roasts"
var lang = response.Item.System.Language; // -> "en-US"
```

See [Working with Strongly Typed Models](./docs/customization-and-extensibility/strongly-typed-models.md) to learn how to generate models and adjust the logic to your needs.

## Further information

For more developer resources, visit:

- [✔️ Best practices for Kontent.ai Delivery SDK for .NET](./docs)
- Kontent.ai Learn:
  - [.NET Tutorials](https://kontent.ai/learn/tutorials/develop-apps?tech=dotnet)
  - [API Reference](https://kontent.ai/learn/reference)

## Get involved

Check out the Contributing page to see the best places to file issues, start discussions, and begin contributing.
