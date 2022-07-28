# Kontent.ai .NET delivery SDK

## Best practices

- ✔️ [Use Dependency Injection](./Configuration/Registering-the-DeliveryClient-to-the-IServiceCollection-in-ASP.NET-Core.md#standard-usage.md) for better app design
- ✔️ [Use `HttpClientFactory`](./Configuration/Registering-the-DeliveryClient-to-the-IServiceCollection-in-ASP.NET-Core.md#httpclientfactory) for increased performance and stability of your app
- ✔️ [Use strongly-typed models](./customization-and-extensibility/Working-with-strongly-typed-models.md) for all the [10 advantages described here](./customization-and-extensibility/Strong-Types-Explained-%E2%80%93-10-Advantages.md)
- ✔️ [Use the code generator](https://github.com/kontent-ai/model-generator-net) to automate things and avoid errors
- ✔️ [Use partial classes for extending the models](./customization-and-extensibility/Partial-class-customization-techniques.md) & not mess up the generated ones
- ✔️ [Use structured rich-text rendering](./customization-and-extensibility/Structured-Rich-text-rendering.md) to enable display templates for rich-text elements
- ✔️ [Enable retry logic](./retrieving-data/Retry-capabilities.md) to ensure maximum resiliency of your app
- ✔️ [Secret Manager or Azure Key Vault](./retrieving-data/Retrieving-secured-and-previewing-unpublished-content.md) to store Secured Content and Preview API keys

## Full Table of Contents

* **Configuration**
  * [Registering the DeliveryClient to the IServiceCollection in ASP.NET Core](./Configuration/Registering-the-DeliveryClient-to-the-IServiceCollection-in-ASP.NET-Core.md)
  * [Accessing data from multiple projects at the same time using named clients](./Configuration/Accessing-Data-From-Multiple-Projects.md)
  * [Loading DeliveryClient settings for apps running on .NET framework (MVC 5 & OWIN)](./Configuration/Loading-DeliveryClient-settings-for-apps-running-on-.NET-framework-(MVC-5-&-OWIN).md)
* **Retrieving data**
  * [Querying content items](./retrieving-data/Querying-content.md)
  * [Enumerating all items](./retrieving-data/Enumerating-all-items.md)
  * [Retrieving secured and previewing unpublished content](./retrieving-data/Retrieving-secured-and-previewing-unpublished-content.md)
  * [Delivery options explained](./retrieving-data/Delivery-options-explained.md)
  * [Retry capabilities](./retrieving-data/Retry-capabilities.md)
  * [Using the Kontent.Ai.Delivery.Rx reactive library](./retrieving-data/Using-the-Kontent.Ai.Delivery.Rx-reactive-library.md)
  * [Caching responses](./retrieving-data/Caching-responses.md)
  * [Using the Image transformations](./retrieving-data/Using-the-Image-transformations.md)
* **Customization and extensibility**
  * [Resolving links to content items](./customization-and-extensibility/Resolving-links-to-content-items.md)
  * [Structured Rich text rendering](./customization-and-extensibility/Structured-Rich-text-rendering.md)
  * [String-based rendering of linked items in Rich text elements](./customization-and-extensibility/String-based-rendering-of-items-in-Rich-text.md)
  * [Working with strongly typed models](./customization-and-extensibility/Working-with-strongly-typed-models.md)
    * [10 Advantages of strong types](./customization-and-extensibility/Strong-Types-Explained-–-10-Advantages.md)
    * [Generating models](./customization-and-extensibility/Strong-Types-Explained-–-Code-Generator.md)
    * [DataAnnotations attributes](./customization-and-extensibility/Strong-Types-Explained-–-DataAnnotations-attributes.md)
    * [Model Inheritance](./customization-and-extensibility/Strong-Types-Explained-–-Model-Inheritance.md)
    * [Runtime Typing](./customization-and-extensibility/Strong-Types-Explained-–-Runtime-Typing.md)
  * [Support for custom types in models via Value Converters](./customization-and-extensibility/Support-for-custom-types-in-models-via-Value-Converters.md)
  * [Retrieve modular content from API response](./customization-and-extensibility/Retrieve-modular-content-from-API-response.md)
  * [Partial class customization techniques](./customization-and-extensibility/Partial-class-customization-techniques.md)
* **Unit testing**
  * [Faking responses](./testing/Faking-responses.md)
* [**Release & version management**](https://github.com/kontent-ai/kontent-ai.github.io/blob/main/docs/articles/Release-%26-version-management-of-.NET-projects.md)
  * [Kontent.ai best practices for .csproj files](https://github.com/kontent-ai/kontent-ai.github.io/blob/main/docs/articles/Kontent.ai-best-practices-for-.csproj-files.md)
* [**Developing plugins**](./testing/Developing-plugins.md)
* **Troubleshooting**
  * [Using Source Link for debugging](./troubleshooting/Using-Source-Link-for-debugging.md)
***



