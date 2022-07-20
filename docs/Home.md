# Best practices for Kontent Delivery SDK for .NET
DOs:

- ✔️ [Use Dependency Injection](https://github.com/Kentico/kontent-delivery-sdk-net/wiki/Registering-the-DeliveryClient-to-the-IServiceCollection-in-ASP.NET-Core#standard-usage) for better app design
- ✔️ [Use `HttpClientFactory`](https://github.com/Kentico/kontent-delivery-sdk-net/wiki/Registering-the-DeliveryClient-to-the-IServiceCollection-in-ASP.NET-Core#httpclientfactory) for increased performance and stability of your app
- ✔️ [Use strongly-typed models](Working-with-strongly-typed-models) for all the [10 advantages described here](Strong-Types-Explained-%E2%80%93-10-Advantages)
- ✔️ [Use the code generator](https://github.com/Kentico/kontent-generators-net) to automate things and avoid errors
- ✔️ [Use partial classes for extending the models](Partial-class-customization-techniques) & not mess up the generated ones
- ✔️ [Use structured rich-text rendering](Structured-Rich-text-rendering) to enable display templates for rich-text elements
- ✔️ [Enable retry logic](Retry-capabilities) to ensure maximum resiliency of your app
- ✔️ [Secret Manager or Azure Key Vault](Retrieving-secured-and-previewing-unpublished-content) to store Secured Content and Preview API keys

DON'Ts:
- ❌ [Use .NET Framework 4.x for new projects](Loading-DeliveryClient-settings-for-apps-running-on-.NET-framework-(MVC-5-&-OWIN)) as it'll be soon replaced with .NET 5 (based on .NET Core)/.NET 6

## Full Table of Contents

* **Configuration**
  * [Registering the DeliveryClient to the IServiceCollection in ASP.NET Core](Registering-the-DeliveryClient-to-the-IServiceCollection-in-ASP.NET-Core)
  * [Accessing data from multiple projects at the same time using named clients](Accessing-Data-From-Multiple-Projects)
  * [Loading DeliveryClient settings for apps running on .NET framework (MVC 5 & OWIN)](Loading-DeliveryClient-settings-for-apps-running-on-.NET-framework-(MVC-5-&-OWIN))
* **Retrieving data**
  * [Querying content items](Querying-content)
  * [Enumerating all items](Enumerating-all-items)
  * [Retrieving secured and previewing unpublished content](Retrieving-secured-and-previewing-unpublished-content)
  * [Retry capabilities](Retry-capabilities)
  * [Using the Kentico.Kontent.Delivery.Rx reactive library](Using-the-Kentico.Kontent.Delivery.Rx-reactive-library)
  * [Caching responses](Caching-responses)
  * [Using the Image transformations](Using-the-Image-transformations)
* **Customization and extensibility**
  * [Resolving links to content items](Resolving-links-to-content-items)
  * [Structured Rich text rendering](Structured-Rich-text-rendering)
  * [String-based rendering of linked items in Rich text elements](String-based-rendering-of-items-in-Rich-text)
  * [Working with strongly typed models](Working-with-strongly-typed-models)
    * [10 Advantages of strong types](Strong-Types-Explained-–-10-Advantages)
    * [Generating models](Strong-Types-Explained-–-Code-Generator)
    * [DataAnnotations attributes](Strong-Types-Explained-–-DataAnnotations-attributes)
    * [Model Inheritance](Strong-Types-Explained-–-Model-Inheritance)
    * [Runtime Typing](Strong-Types-Explained-–-Runtime-Typing)
  * [Support for custom types in models via Value Converters](Support-for-custom-types-in-models-via-Value-Converters)
  * [Retrieve modular content from API response](https://github.com/Kentico/kontent-delivery-sdk-net/wiki/Retrieve-modular-content-from-API-response)
* **Unit testing**
  * [Faking responses](Faking-responses)
* [**Release & version management**](https://github.com/Kentico/Home/wiki/Release-&-version-management-of-.NET-projects)
  * [Kentico's best practices for .csproj files](https://github.com/Kentico/Home/wiki/Kentico's-best-practices-for-.csproj-files)
* [**Developing plugins**](Developing-plugins)
* **Troubleshooting**
  * [Using Source Link for debugging](Using-Source-Link-for-debugging)
***
