# Kontent.ai .NET delivery SDK

## Best practices

- ✔️ [Use Dependency Injection](./configuration/dependency-injection.md#standard-usage.md) for better app design
- ✔️ [Use `HttpClientFactory`](./configuration/dependency-injection.md#httpclientfactory) for increased performance and stability of your app
- ✔️ [Use strongly-typed models](./customization-and-extensibility/strongly-typed-models.md) for all the [10 advantages described here](./customization-and-extensibility/strong-types-explained/10-advantages.md)
- ✔️ [Use the code generator](https://github.com/kontent-ai/model-generator-net) to automate things and avoid errors
- ✔️ [Use partial classes for extending the models](./customization-and-extensibility/customization-techniques.md) & not mess up the generated ones
- ✔️ [Use structured rich-text rendering](./customization-and-extensibility/structured-models/structured-models-rendering.md) to enable display templates for rich-text elements
- ✔️ [Enable retry logic](./configuration/retry-policy.md) to ensure maximum resiliency of your app
- ✔️ [Secret Manager or Azure Key Vault](./retrieving-data/secure-and-preview-api.md) to store Secured Content and Preview API keys

## Full Table of Contents

- **Configuration**
  - [Registering the DeliveryClient to the IServiceCollection in ASP.NET Core](./configuration/dependency-injection.md)
  - [Accessing data from multiple projects at the same time using named clients](./configuration/multiple-delivery-clients.md)
  - [Retry capabilities](./configuration/retry-policy.md)
  - [Delivery options explained](./configuration/delivery-options.md)
- **Retrieving data**
  - [Querying content items](./retrieving-data/querying-content.md)
  - [Enumerating all items](./retrieving-data/items-feed.md)
  - [Retrieving secured and previewing unpublished content](./retrieving-data/secure-and-preview-api.md)
  - [Using the Kontent.Ai.Delivery.Rx reactive library](./retrieving-data/reactive-library.md)
  - [Caching responses](./retrieving-data/caching.md)
  - [Using the Image transformations](./retrieving-data/image-transformation.md)
- **Customization and extensibility**
  - [Resolving links to content items](./customization-and-extensibility/structured-models/resolving-item-links.md)
  - [Structured Rich text rendering](./customization-and-extensibility/structured-models/structured-models-rendering.md)
  - [String-based rendering of linked items in Rich text elements](./customization-and-extensibility/structured-models/string-based-linked-items-rendering.md)
  - [Working with strongly typed models](./customization-and-extensibility/strongly-typed-models.md)
    - [10 Advantages of strong types](./customization-and-extensibility/strong-types-explained/10-advantages.md)
    - [Generating models](./customization-and-extensibility/strong-types-explained/code-generator.md)
    - [DataAnnotations attributes](./customization-and-extensibility/strong-types-explained/dataannotation-attributes.md)
    - [Model Inheritance](./customization-and-extensibility/strong-types-explained/model-inheritance.md)
    - [Runtime Typing](./customization-and-extensibility/strong-types-explained/runtime-typing.md)
  - [Support for custom types in models via Value Converters](./customization-and-extensibility/value-converters.md)
  - [Retrieve modular content from API response](./customization-and-extensibility/modular-content-in-response.md)
  - [Partial class customization techniques](./customization-and-extensibility/customization-techniques.md)
- **Unit testing**
  - [Faking responses](./testing/faking.md)
- **Repository practices**
  - [Release & version management](https://kontent-ai.github.io/ci-and-automation/net-guidelines/Kontent.ai-best-practices-for-.csproj-files#version-management)
  - [Kontent.ai best practices for .csproj files](https://kontent-ai.github.io/ci-and-automation/net-guidelines/Kontent.ai-best-practices-for-.csproj-files)
- **Tools using SDK**
  - [Source tracking header](./testing/tracking.md)
- **Troubleshooting**
  - [Using Source Link for debugging](./troubleshooting/source-link.md)
