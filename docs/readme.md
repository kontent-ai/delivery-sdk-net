# Kontent.ai .NET delivery SDK

## Best practices

- ✔️ [Use Dependency Injection](./Configuration/Registering-the-DeliveryClient-to-the-IServiceCollection-in-ASP.NET-Core.md#standard-usage.md) for better app design
- ✔️ [Use `HttpClientFactory`](./Configuration/Registering-the-DeliveryClient-to-the-IServiceCollection-in-ASP.NET-Core.md#httpclientfactory) for increased performance and stability of your app
- ✔️ [Use strongly-typed models](./Customization%20and%20extensibility/Working-with-strongly-typed-models.md) for all the [10 advantages described here](Strong-Types-Explained-%E2%80%93-10-Advantages.md)
- ✔️ [Use the code generator](https://github.com/kontent-ai/model-generator-net) to automate things and avoid errors
- ✔️ [Use partial classes for extending the models](./Customization%20and%20extensibility/Partial-class-customization-techniques.md) & not mess up the generated ones
- ✔️ [Use structured rich-text rendering](./Customization%20and%20extensibility/Structured-Rich-text-rendering.md) to enable display templates for rich-text elements
- ✔️ [Enable retry logic](./Retrieving%20data/Retry-capabilities.md) to ensure maximum resiliency of your app
- ✔️ [Secret Manager or Azure Key Vault](./Retrieving%20data/Retrieving-secured-and-previewing-unpublished-content.md) to store Secured Content and Preview API keys

## Full Table of Contents

* **Configuration**
  * [Registering the DeliveryClient to the IServiceCollection in ASP.NET Core](./Configuration/Registering-the-DeliveryClient-to-the-IServiceCollection-in-ASP.NET-Core.net.md)
  * [Accessing data from multiple projects at the same time using named clients](./Configuration/Accessing-Data-From-Multiple-Projects.net.md)
  * [Loading DeliveryClient settings for apps running on .NET framework (MVC 5 & OWIN)](./Configuration/Loading-DeliveryClient-settings-for-apps-running-on-.NET-framework-(MVC-5-&-OWIN).md)
* **Retrieving data**
  * [Querying content items](./Retrieving%20data/Querying-content.md)
  * [Enumerating all items](./Retrieving%20data/Enumerating-all-items.md)
  * [Retrieving secured and previewing unpublished content](./Retrieving%20data/Retrieving-secured-and-previewing-unpublished-content.md)
  * [Delivery options explained](./Retrieving%20data/Delivery-options-explained.md)
  * [Retry capabilities](./Retrieving%20data/Retry-capabilities.md)
  * [Using the Kontent.Ai.Delivery.Rx reactive library](./Retrieving%20data/Using-the-Kontent.Ai.Delivery.Rx-reactive-library.md)
  * [Caching responses](./Retrieving%20data/Caching-responses.md)
  * [Using the Image transformations](./Retrieving%20data/Using-the-Image-transformations.md)
* **Customization and extensibility**
  * [Resolving links to content items](./Customization%20and%20extensibility/Resolving-links-to-content-items.md)
  * [Structured Rich text rendering](./Customization%20and%20extensibility/Structured-Rich-text-rendering.md)
  * [String-based rendering of linked items in Rich text elements](./Customization%20and%20extensibility/String-based-rendering-of-items-in-Rich-text.md)
  * [Working with strongly typed models](./Customization%20and%20extensibility/Working-with-strongly-typed-models.md)
    * [10 Advantages of strong types](./Customization%20and%20extensibility/Strong-Types-Explained-–-10-Advantages.md)
    * [Generating models](./Customization%20and%20extensibility/Strong-Types-Explained-–-Code-Generator.md)
    * [DataAnnotations attributes](./Customization%20and%20extensibility/Strong-Types-Explained-–-DataAnnotations-attributes.md)
    * [Model Inheritance](./Customization%20and%20extensibility/Strong-Types-Explained-–-Model-Inheritance.md)
    * [Runtime Typing](./Customization%20and%20extensibility/Strong-Types-Explained-–-Runtime-Typing.md)
  * [Support for custom types in models via Value Converters](./Customization%20and%20extensibility/Support-for-custom-types-in-models-via-Value-Converters.md)
  * [Retrieve modular content from API response](./Customization%20and%20extensibility/Retrieve-modular-content-from-API-response.md)
  * [Partial class customization techniques](./Customization%20and%20extensibility/Partial-class-customization-techniques.md)
* **Unit testing**
  * [Faking responses](./Unit%20testing/Faking-responses.md)
* [**Release & version management**](https://github.com/kontent-ai/kontent-ai.github.io/blob/main/docs/articles/Release-%26-version-management-of-.NET-projects.md)
  * [Kontent.ai best practices for .csproj files](https://github.com/kontent-ai/kontent-ai.github.io/blob/main/docs/articles/Kontent.ai-best-practices-for-.csproj-files.md)
* [**Developing plugins**](./Unit%20testing/Developing-plugins.md)
* **Troubleshooting**
  * [Using Source Link for debugging](./Troubleshooting/Using-Source-Link-for-debugging.md)
***



