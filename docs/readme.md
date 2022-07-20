# Kontent.ai .NET delivery SDK

## Best practices
### DOs:

- ✔️ [Use Dependency Injection](https://github.com/kontent-ai/delivery-sdk-net/blob/migration/docs/Registering-the-DeliveryClient-to-the-IServiceCollection-in-ASP.NET-Core.md#standard-usage) for better app design
- ✔️ [Use `HttpClientFactory`](https://github.com/kontent-ai/delivery-sdk-net/blob/migration/Registering-the-DeliveryClient-to-the-IServiceCollection-in-ASP.NET-Core.md#httpclientfactory) for increased performance and stability of your app
- ✔️ [Use strongly-typed models](Working-with-strongly-typed-models.md) for all the [10 advantages described here](Strong-Types-Explained-%E2%80%93-10-Advantages.md)
- ✔️ [Use the code generator](https://github.com/kontent-ai/model-generator-net) to automate things and avoid errors
- ✔️ [Use partial classes for extending the models](Partial-class-customization-techniques.md) & not mess up the generated ones
- ✔️ [Use structured rich-text rendering](Structured-Rich-text-rendering.md) to enable display templates for rich-text elements
- ✔️ [Enable retry logic](Retry-capabilities.md) to ensure maximum resiliency of your app
- ✔️ [Secret Manager or Azure Key Vault](Retrieving-secured-and-previewing-unpublished-content.md) to store Secured Content and Preview API keys

### DON'Ts:
- ❌ [Use .NET Framework 4.x for new projects](Loading-DeliveryClient-settings-for-apps-running-on-.NET-framework-(MVC-5-&-OWIN)) as it'll be soon replaced with .NET 5 (based on .NET Core)/.NET 6

## Full Table of Contents

* **Configuration**
  * [Registering the DeliveryClient to the IServiceCollection in ASP.NET Core](Registering-the-DeliveryClient-to-the-IServiceCollection-in-ASP.NET-Core.net)
  * [Accessing data from multiple projects at the same time using named clients](Accessing-Data-From-Multiple-Projects.net)
  * [Loading DeliveryClient settings for apps running on .NET framework (MVC 5 & OWIN)](Loading-DeliveryClient-settings-for-apps-running-on-.NET-framework-(MVC-5-&-OWIN).md)
* **Retrieving data**
  * [Querying content items](Querying-content.md)
  * [Enumerating all items](Enumerating-all-items.md)
  * [Retrieving secured and previewing unpublished content](Retrieving-secured-and-previewing-unpublished-content.md)
  * [Retry capabilities](Retry-capabilities.md)
  * [Using the Kontent.Ai.Delivery.Rx reactive library](Using-the-Kontent.Ai.Delivery.Rx-reactive-library.md)
  * [Caching responses](Caching-responses.md)
  * [Using the Image transformations](Using-the-Image-transformations.md)
* **Customization and extensibility**
  * [Resolving links to content items](Resolving-links-to-content-items.md)
  * [Structured Rich text rendering](Structured-Rich-text-rendering.md)
  * [String-based rendering of linked items in Rich text elements](String-based-rendering-of-items-in-Rich-text.md)
  * [Working with strongly typed models](Working-with-strongly-typed-models.md)
    * [10 Advantages of strong types](Strong-Types-Explained-–-10-Advantages.md)
    * [Generating models](Strong-Types-Explained-–-Code-Generator.md)
    * [DataAnnotations attributes](Strong-Types-Explained-–-DataAnnotations-attributes.md)
    * [Model Inheritance](Strong-Types-Explained-–-Model-Inheritance.md)
    * [Runtime Typing](Strong-Types-Explained-–-Runtime-Typing.md)
  * [Support for custom types in models via Value Converters](Support-for-custom-types-in-models-via-Value-Converters.md)
  * [Retrieve modular content from API response](Retrieve-modular-content-from-API-response.md)
* **Unit testing**
  * [Faking responses](Faking-responses.md)
* [**Release & version management**](https://github.com/kontent-ai/kontent-ai.github.io/blob/main/docs/articles/Release-%26-version-management-of-.NET-projects.md)
  * [Kentico's best practices for .csproj files](https://github.com/kontent-ai/kontent-ai.github.io/blob/main/docs/articles/Kontent.ai-best-practices-for-.csproj-files.md)
* [**Developing plugins**](Developing-plugins.md)
* **Troubleshooting**
  * [Using Source Link for debugging](Using-Source-Link-for-debugging.md)
***



