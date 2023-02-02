## Contents

<!-- TOC -->

- [Contents](#contents)
- [Strongly-typed models](#strongly-typed-models)
- [Defining a model](#defining-a-model)
  - [Typing the properties](#typing-the-properties)
    - [Typing simple elements](#typing-simple-elements)
    - [Typing Linked Items](#typing-linked-items)
    - [Typing Rich text](#typing-rich-text)
    - [Typing Custom elements](#typing-custom-elements)
  - [Naming the properties](#naming-the-properties)
  - [Examples](#examples)
- [Generating models](#generating-models)
- [Retrieving content items](#retrieving-content-items)
- [Customizing the strong-type binding logic](#customizing-the-strong-type-binding-logic)
  - [Adding support for runtime type resolution](#adding-support-for-runtime-type-resolution)
    - [Generating custom type providers](#generating-custom-type-providers)
  - [Customizing the property matching](#customizing-the-property-matching)
  - [Customizing the strong-type resolution mechanism](#customizing-the-strong-type-resolution-mechanism)

<!-- /TOC -->

## Strongly-typed models
The `IDeliveryClient` interface supports fetching of strongly-typed models.

```csharp
// Initializes a client
IDeliveryClient deliveryClient = DeliveryClientBuilder
    .WithProjectId("975bf280-fd91-488c-994c-2f04416e5ee3")
    .Build();

// Basic retrieval
deliveryClient.GetItemAsync("article_about_coffee");

// Strongly-typed model retrieval
deliveryClient.GetItemAsync<Article>("article_about_coffee");
```

This approach is beneficial for its:

- type safety during compile-time
- convenience of usage by a developer (`@Article.ArticleTitle` vs. `@Article.GetString("article_title")`)
- support of type-dependent functionalities (such as [display templates](http://www.growingwiththeweb.com/2012/12/aspnet-mvc-display-and-editor-templates.html) in MVC)

## Defining a model

The models are simple [POCO](https://en.wikipedia.org/wiki/Plain_old_CLR_object) classes, which means they don't have any attached behavior or dependency on an external framework.

### Typing the properties

#### Typing simple elements

Here are the data types you can use for different content type elements:

- [built-in types](https://msdn.microsoft.com/en-us/library/ya5y69ds.aspx) such as `string`, `decimal` and their nullable equivalents for simple elements like Number or Text.
- `IEnumerable<Kontent.Ai.Delivery.Abstractions.IMultipleChoiceOption>` for Multiple choice elements.
- `IEnumerable<Kontent.Ai.Delivery.Abstractions.IAsset>` for Asset elements.
- `IEnumerable<Kontent.Ai.Delivery.Abstractions.ITaxonomyTerm>` for Taxonomy elements.

#### Typing Linked Items

For linked items elements, use either `IEnumerable<T>` or any concrete implementation of `ICollection<T>` such as `List<T>` or `HashSet<T>`.

Depending on your scenario, use one of the following as the data type parameter `<T>`:

- Specific content type model (e.g., `Article`) - when the element contains content items based on a single content type.
- `object` - when the element contains mixed content types and you want the objects to be strongly-typed. See [Adding support for runtime type resolution](#adding-support-for-runtime-type-resolution) for more details.

#### Typing Rich text

For Rich text elements, use either `string` to receive HTML code resolved using string-based resolver as outlined in [Rendering linked items in Rich text](./structured-models/string-based-linked-items-rendering.md) or `IRichTextContent` to retrieve rich text content as [structured data](./structured-models/structured-model-rendering.md).

#### Typing Date and time

For Date and time elements, use either `DateTime?` to pure DateTime? value or `IDateTimeContent` to retrieve DateTime as [structured data](./structured-models/structured-model-rendering.md).

#### Typing Custom elements

For [Custom elements](https://docs.kontent.ai/tutorials/develop-apps/integrate/integrating-your-own-content-editing-features), use the `string` type. Custom elements behave as simple text-based elements.

### Naming the properties

By default, the model properties and content elements are matched by codenames of the elements. The SDK tries to convert the element codenames to [PascalCase](https://msdn.microsoft.com/en-us/library/x2dbyw72(v=vs.71)). For example, a content element with the codename of `article_title` translates to a property called `ArticleTitle`.

If you need to change the codename of an element that a property is bound to, you can enrich the property with the `Kontent.Ai.Delivery.ContentItems.PropertyNameAttribute` attribute.

```csharp
[PropertyName("text_field")]
public string ArticleTitle { get; set; }
```

If you also want to set the behavior of the Newtonsoft and don't want to map the same element to different properties, you can use the `Newtonsoft.Json.JsonPropertyAttribute`.

```csharp
[JsonProperty("text_field")]
public string ArticleTitle { get; set; }
```

### Examples

You can find sample models at <https://github.com/kontent-ai/delivery-sdk-net/tree/master/Kontent.Ai.Delivery.Tests/Models>

## Generating models

:information_source: You can save time by [generating content type models](./strong-types-explained/code-generator.md) for your project using the [Kontent.ai .NET code generator](https://github.com/kontent-ai/model-generator-net) utility.

## Retrieving content items

Both the `GetItemAsync` and `GetItemsAsync` client methods have their corresponding generic overloads, `GetItemAsync<T>` and `GetItemsAsync<T>`. The parameters are the same as for the non-generic variants. The only difference is that you need to specify the type parameter `<T>`.

You can either specify the type directly (e.g., `GetItemAsync<Article>`) or pass the type as the `object` class (e.g., `GetItemAsync<object>`). Use the second approach if you don't know what the type is to let the SDK resolve it during runtime.

This parameter represents the model you want to load. You can specify the parameter in two ways:

- by using a content type model, for example, `GetItemAsync<Article>`
- by passing the [`object`](https://msdn.microsoft.com/en-us/library/system.object(v=vs.110)) class, for example, `GetItemAsync<object>`

Use the second approach if you don't know what the content type will be and you want the application to [resolve content types during runtime](#adding-support-for-runtime-type-resolution).

## Customizing the strong-type binding logic

### Adding support for runtime type resolution

The `IDeliveryClient` instance supports runtime type resolution. This means you can pass the [`object`](https://msdn.microsoft.com/en-us/library/system.object(v=vs.110)) class instead of explicitly specifying the data type in the model or when calling the `GetItemAsync<>` method. The data type will be resolved dynamically during runtime.

For example:

```csharp
object model = await client.GetItemAsync<object>(...);
Type type = model.Item.GetType(); // type will be e.g. 'Article'
```

For this to work, the SDK needs to know the mappings between the content types and your models.

If you want to use the runtime type resolution in your application, you need to implement the [`Kontent.Ai.Delivery.Abstractions.ITypeProvider`](../../Kontent.Ai.Delivery.Abstractions/ContentItems/ITypeProvider.cs) interface.

```csharp
public class CustomTypeProvider : ITypeProvider
{
  public Type GetType(string contentType)
  {
    switch(contentType)
    {
      case "article":
        return typeof(Article);
      case "office":
        return typeof(Office);

      ...

      default:
        return null;
    }
  }
}
```

Next, you either register the type provider within `IServiceCollection`

```csharp
services.AddSingleton<ITypeProvider, CustomTypeProvider>();
services.AddDeliveryClient(Configuration);
```

or pass it to the `DeliveryClientBuilder`.

```csharp
CustomTypeProvider customTypeProvider = new CustomTypeProvider();
IDeliveryClient client = DeliveryClientBuilder
    .WithProjectId("975bf280-fd91-488c-994c-2f04416e5ee3")
    .WithTypeProvider(customTypeProvider)
    .Build();
```

#### Generating custom type providers

Similarly to models, you can generate implementations of `ITypeProvider` by using the [.NET code generator tool](https://github.com/kontent-ai/model-generator-net) and specifying the `--withtypeprovider true` parameter. Generated `CustomTypeProvider` interface implementation is extensible - you can customize it by overriding/overloading its members.

### Customizing the property matching
Currently, the automatic matching is based on the codenames being converted to [PascalCase](https://msdn.microsoft.com/en-us/library/x2dbyw72(v=vs.71) "PascalCase").

If you want to customize the way content elements and model properties are matched, you need to implement the [`Kontent.Ai.Delivery.Abstractions.IPropertyMapper`](../../Kontent.Ai.Delivery.Abstractions/ContentItems/IPropertyMapper.cs) interface and pass it to the `DeliveryClientBuilder` class.

```csharp
// Implements a custom property mapper
public class CustomPropertyMapper : IPropertyMapper
{
  public bool IsMatch(PropertyInfo modelProperty, string fieldName, string contentType)
  {
    // Add your logic here
    return modelProperty.Name.ToLower() == fieldName;
  }
}

// Registers the custom property mapper to the IServiceCollection or other framework you are using for dependency injection
services
    .AddSingleton<IPropertyMapper, CustomPropertyMapper>();
    .AddDeliveryClient(Configuration);

// Registers the custom property mapper within a delivery client using builder
CustomPropertyMapper customPropertyMapper = new CustomPropertyMapper();
IDeliveryClient client = DeliveryClientBuilder
    .WithProjectId("975bf280-fd91-488c-994c-2f04416e5ee3")
    .WithPropertyMapper(customPropertyMapper)
    .Build();
```

### Customizing the strong-type resolution mechanism

To replace the whole model instantiation mechanism, you need to implement the [`Kontent.Ai.Delivery.Abstractions.IModelProvider`](../../Kontent.Ai.Delivery.Abstractions/ContentItems/IModelProvider.cs) interface. It contains a single member:

```csharp
Task<T> GetContentItemModelAsync<T>(object item, IEnumerable linkedItems);
```

The objective is to create an instance of `<T>` by utilizing the data provided in the `item` and `linkedItems` properties.
