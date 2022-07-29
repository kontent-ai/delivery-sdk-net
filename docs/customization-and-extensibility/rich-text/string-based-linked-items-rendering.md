## Contents

<!-- TOC -->

- [Contents](#contents)
- [Introduction](#introduction)
- [Content items and components in Rich text](#content-items-and-components-in-rich-text)
- [Defining a content type model](#defining-a-content-type-model)
- [Implementing a resolver](#implementing-a-resolver)
- [Registering a resolver](#registering-a-resolver)
  - [Custom default resolver](#custom-default-resolver)
- [Retrieving Rich text content](#retrieving-rich-text-content)

<!-- /TOC -->

## Introduction

The following information applies to Rich text model properties typed as `string`. In case you are using structured data model type `IRichTextContent`, see [Structured Rich text rendering](./structured-rich-text-rendering.md)

## Content items and components in Rich text

[Rich text elements](https://docs.kontent.ai/tutorials/write-and-collaborate/write-content/composing-content-in-the-rich-text-editor#adding-components) in Kontent.ai can contain components and other content items. For example, if you write a blog post, you might want to insert a video or testimonial to a specific place in your article.

Without adjusting your application, any component or content item in a Rich text element will resolve to an empty object reference, which won't be rendered on the page.

```html
<object type="application/kenticocloud" data-type="item" data-codename="donate_with_us"></object>
```

To display the component or content item in the rich text on your website, you need to define how it should be rendered:

1. [Define a model](#defining-a-content-type-model) for the content type of the item or component.
2. [Implement a resolver](#implementing-a-resolver) of inline content.
3. [Register the resolver](#registering-a-resolver) within the `IDeliveryClient` instance.
4. [Retrieve content](#retrieving-rich-text-content) of a Rich text element.

For example, let's say you want to add YouTube videos to your article. In such case, you would need a content type *YouTube video* with a single Text element for the *Video ID*.

To learn more about content items and components in rich text, see our [API Reference](https://kontent.ai/learn/tutorials/write-and-collaborate/structure-your-content/structure-your-content/).

## Defining a content type model

First, you need to define a strongly typed model of the *YouTube video* content type. For more information about models, see [Working with strongly typed models](../strongly-typed-models.md).

For example, a model for the *YouTube video* content type can look like this:

```csharp
namespace DancingGoat.Models.ContentTypes
{
    public class YoutubeVideo
    {
        public string VideoId { get; set; }
    }
}
```

You will also want to register your new model in your implementation of the [ITypeProvider](https://github.com/kontent-ai/delivery-sdk-net/Kontent.Ai.Delivery/StrongTyping/ITypeProvider.cs) interface. See an example in our [sample application](https://github.com/kontent-ai/sample-app-net/DancingGoat/Models/ContentTypes/CustomTypeProvider.cs).

```csharp
      ...
      case "youtube_video":
          return typeof(YoutubeVideo);
      ...
```

 > **Tip**: To save time, use the [Kontent.ai .NET code generator](https://github.com/kontent-ai/model-generator-net) and have the strongly typed models of your project's content types generated for you.

## Implementing a resolver

Your resolver must implement the `IInlineContentItemsResolver` interface, which defines the `Resolve()` method for resolving components and linked items to HTML markup.

**Note**: Make sure the resolver returns a valid HTML5 fragment. Providing an invalid HTML5 fragment can cause your application to render the item or component incorrectly.

```csharp
// Sample resolver implementation
public class YoutubeVideoResolver : IInlineContentItemsResolver<YoutubeVideo>
    {
        public string Resolve(YoutubeVideo data)
        {
            return
                $"<div><iframe type=\"text/html\" width=\"640\" height=\"385\" style=\"display:block; margin: auto; margin-top:30px ; margin-bottom: 30px\" src=\"https://www.youtube.com/embed/{data.VideoId}?autoplay=1\" frameborder=\"0\"></iframe></div>";
        }
    }
```

If the resolver or the content item itself is not available, the object reference is replaced by an empty string (in a live environment) or an appropriate error message (in a preview environment).

When are content items available?

* For live environment, a content item is available when published, and unavailable when deleted or unpublished.
* For preview environment, a content item is available when it exists in the project inventory, and unavailable when deleted.

Components are always available, as they are part of the Rich text element.

## Registering a resolver

Once you implement the resolver, you need to either register it in `IServiceCollection`

```csharp
// Registers the resolver and provider in IServiceCollection
// or another framework you are using for dependency injection
services
    .AddDeliveryInlineContentItemsResolver<YoutubeVideo, YoutubeVideoResolver>();
    .AddSingleton<ITypeProvider, CustomTypeProvider>();
    .AddDeliveryClient(Configuration);
```
or within the `IDeliveryClient` instance by using the `DeliveryClientBuilder` class.

```csharp

IDeliveryClient client = DeliveryClientBuilder
    .WithProjectId("975bf280-fd91-488c-994c-2f04416e5ee3")
    // Registers inline content item resolver for the 'YouTube video' type
    .WithInlineContentItemsResolver(new YoutubeVideoResolver())
    // Registers strongly-typed models
    .WithTypeProvider(new CustomTypeProvider())
    .Build();
```

### Custom default resolver

If you need to customize the application behavior for cases when no resolver exists for a specific content type, you need to implement a resolver for the `object` type and register it as the default resolver.

```csharp
// Sample default resolver
public class MyDefaultResolver : IInlineContentItemsResolver<object>
{
    public string Resolve(object data)
    {
            return "Content not available.";
    }
}
```

Once you implement the resolver, you need to either register it in `IServiceCollection`

```csharp
services
    .AddDeliveryInlineContentItemsResolver<object, MyDefaultResolver>();
    .AddDeliveryClient(Configuration);
```

or within the `IDeliveryClient` instance by using the `DeliveryClientBuilder` class.

```csharp
IDeliveryClient client = DeliveryClientBuilder
    .WithProjectId("975bf280-fd91-488c-994c-2f04416e5ee3")
    // Registers a custom resolver as the default resolver
    .WithInlineContentItemsResolver(new MyDefaultResolver())
    .Build();
```

## Retrieving Rich text content

Now, when you retrieve content of a Rich text element via a strongly-typed property, content items and components based on the *YouTube video* type will be resolved correctly.

```csharp
// Retrieves the 'Coffee beverages explained' article
IDeliveryItemResponse response = await client.GetItemAsync<Article>("coffee_beverages_explained");
Article article = response.Item;

// Retrieves text from the 'body_copy' Rich text element
string articleBody = article.BodyCopy;
```

The HTML output of your content item resolver is now included in the Rich text.

```html
<div><iframe type=\"text/html\" width=\"640\" height=\"385\" style=\"display:block; margin: auto; margin-top:30px ; margin-bottom: 30px\" src=\"https://www.youtube.com/embed/wZZ7oFKsKzY?autoplay=1\" frameborder=\"0\"></iframe></div>
```
