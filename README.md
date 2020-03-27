# Kentico Kontent Delivery .NET SDK

[![Build status](https://ci.appveyor.com/api/projects/status/3m3q2ads2y43bh9o/branch/master?svg=true)](https://ci.appveyor.com/project/kentico/deliver-net-sdk/branch/master)
[![CircleCI](https://circleci.com/gh/Kentico/kontent-delivery-sdk-net.svg?style=shield)](https://circleci.com/gh/Kentico/kontent-delivery-sdk-net)
[![codecov](https://codecov.io/gh/Kentico/kontent-delivery-sdk-net/branch/master/graph/badge.svg)](https://codecov.io/gh/Kentico/kontent-delivery-sdk-net)
[![Stack Overflow](https://img.shields.io/badge/Stack%20Overflow-ASK%20NOW-FE7A16.svg?logo=stackoverflow&logoColor=white)](https://stackoverflow.com/tags/kentico-kontent)

| Paradigm        | Package | Downloads | Compatibility | Documentation |
| ------------- |:-------------:| :-------------:|  :-------------:|  :-------------:|
| Async         | [![NuGet](https://img.shields.io/nuget/v/Kentico.Kontent.Delivery.svg)](https://www.nuget.org/packages/Kentico.Kontent.Delivery) | [![NuGet](https://img.shields.io/nuget/dt/Kentico.Kontent.delivery.svg)](https://www.nuget.org/packages/Kentico.Kontent.Delivery) | [`netstandard2.0`](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) | [📖](#using-the-deliveryclient) |
| Reactive      | [![NuGet](https://img.shields.io/nuget/v/Kentico.Kontent.Delivery.Rx.svg)](https://www.nuget.org/packages/Kentico.Kontent.Delivery.Rx) | [![NuGet](https://img.shields.io/nuget/dt/Kentico.Kontent.delivery.Rx.svg)](https://www.nuget.org/packages/Kentico.Kontent.Delivery.Rx) | [`netstandard2.0`](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) | [📖](../../wiki/Using-the-Kentico.Kontent.Delivery.Rx-reactive-library) |

## Summary

The Kentico Kontent Delivery .NET SDK is a client library that lets you easily retrieve content from Kentico Kontent.

### Getting started

Installation via Package Manager Console in Visual Studio:
```powershell
PM> Install-Package Kentico.Kontent.Delivery 
```

Installation via .NET CLI:
```console
> dotnet add package Kentico.Kontent.Delivery 
```

## Usage
The `IDeliveryClient` interface is the main interface of the SDK. Using an implementation of this interface, you can retrieve content from your Kentico Kontent projects.

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

In this case, the SDK reads the configuration from the `DeliveryOptions` section of the `Configuration` object. There are many ways of providing the configuration to the `DeliveryClient` as well as many advanced registration scenarios which you can all find in the [Wiki](../../wiki/Registering-the-DeliveryClient-to-the-IServiceCollection-in-ASP.NET-Core).

To see a complete working example, go to one of our sample sites:
- [Kentico Kontent ASP.NET Core MVC](https://github.com/Kentico/kontent-sample-app-net) or
- [Kentico Kontent ASP.NET Core Razor Pages](https://github.com/Kentico/kontent-sample-app-razorpages)

To spin up a fully configured blank site quickly, use the:
- [Kentico Kontent ASP.NET boilerplate](https://github.com/Kentico/kontent-boilerplate-net)


### Usage without IoC/DI containers (ideal for console apps, unit tests...)
You can also set up a `DeliveryOptions` manually using the [`DeliveryClientBuilder`](https://github.com/Kentico/kontent-delivery-sdk-net/blob/master/Kentico.Kontent.Delivery/Builders/DeliveryOptions/DeliveryOptionsBuilder.cs).

```csharp
IDeliveryClient _client = DeliveryClientBuilder
    .WithOptions(builder => builder
        .WithProjectId("<YOUR_PROJECT_ID>")
	.UseProductionApi()
	.Build())
    .Build();
```

### Your first request

Use the [.NET code generator](https://github.com/Kentico/kontent-generators-net) to generate POCO models:

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

Call the `IDeliveryClient` to retrieve data from Kentico Kontent:
```csharp
// Retrieving a single content item
var response = await _client.GetItemAsync<Article>("<article_codename>");
var title = response.Item.Title; // -> "On Roasts"
var lang = response.Item.System.Language; // -> "en-US"
```

See [Working with Strongly Typed Models](../../wiki/Working-with-strongly-typed-models) to learn how to generate models and adjust the logic to your needs.

## Further information

For more developer resources, visit:
* [✔️ Best practices for Delivery SDK for .NET](../../wiki)
* Kentico Kontent Developer Hub:
  * [.NET Tutorials](https://docs.kontent.ai/tutorials/develop-apps)
  * [API Reference](https://docs.kontent.ai/reference)


## Get involved

Check out the [contributing](CONTRIBUTING.md) page to see the best places to file issues, start discussions, and begin contributing.

![Analytics](https://kentico-ga-beacon.azurewebsites.net/api/UA-69014260-4/Kentico/kontent-delivery-sdk-net?pixel)
