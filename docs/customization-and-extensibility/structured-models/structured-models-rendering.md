## Introduction

The [Rich text element](https://docs.kontent.ai/reference/delivery-api#section/Rich-text-element) can contain images, links to content items, content items, and components. The [Date and time element](https://kontent.ai/learn/reference/openapi/delivery-api/#section/Date-and-time-element) can contain display timezone. Your application needs to define how these objects are rendered.

This information applies to Rich text model properties typed as `IRichTextContent` and Date and time model properties typed as `IDateTimeContent`. For rich text model in a case you are using `string` data model, see [String-based rendering of linked items in Rich text](./string-based-linked-items-rendering.md). You can influence which approach to use by [applying](https://github.com/kontent-ai/model-generator-net#delivery-api-parameters) the `--structuredmodel "<structured-model>"` parameter during model generation.

Resolving links to content items is covered in a [separate article](./resolving-item-links.md).

## Structured Rich text data model

`IRichTextContent` provides a list of blocks of one of the following types:
* `IInlineImage`
* `IHtmlContent`
* `IInlineContentItem`

By iterating over the blocks, you can render the Rich text content.

Replaces `string` type of a rich text.

## Rich text rendering in MVC

For rendering of rich text in MVC, you can use the standard approach with `Html.DisplayFor(model => model.RichTextProperty)`.

Individual blocks are rendered with their default HTML representation which matches the content provided by the Delivery API.

You can provide display templates for the following block types:
* **Content items and components (required)** - Display template named `<ModelTypeName>.cshtml` where `ModelTypeName` is based on the content item or component type, e.g., `Tweet.cshtml`.
  * Items and components are resolved using the same mechanism, your application does not need to differentiate them.
  * You can learn more about the differences between items and components in our [API Reference](https://docs.kontent.ai/reference/delivery-api#tag/Linked-content-and-components).
* **Inline images (optional)** - Display template named `InlineImage.cshtml`.

Display templates can be provided to all MVC Controllers through the Shared folder, or relatively based on the current controller.

## Example
![](https://pbs.twimg.com/media/DIFVESkXsAQ8av9.jpg:large)

**[Models/ContentTypes/Article.cs](https://github.com/kontent-ai/sample-app-net/DancingGoat/Models/ContentTypes/Article.cs)** - Generated

```csharp
    public partial class Article
    {
...
        public IRichTextContent BodyCopy { get; set; }
...
    }
```

**[Views/Articles/Show.cshtml](https://github.com/kontent-ai/sample-app-net/DancingGoat/Views/Articles/Show.cshtml)**

```csharp
    <div class="row">
        <div class="article-detail-content">
            @Html.DisplayFor(vm => vm.BodyCopy)
        </div>
    </div>
```

**[Views/Shared/DisplayTemplates/HostedVideo.cshtml](https://github.com/kontent-ai/sample-app-net/DancingGoat/Views/Shared/DisplayTemplates/HostedVideo.cshtml)**
```csharp
@model DancingGoat.Models.HostedVideo

@{ 
    var host = Model.VideoHost.FirstOrDefault()?.Codename;
    if (host == "vimeo") {
        <iframe class="hosted-video__wrapper"
                src="https://player.vimeo.com/video/@(Model.VideoId)?title =0&byline =0&portrait =0"
                width="640"
                height="360"
                frameborder="0"
                webkitallowfullscreen
                mozallowfullscreen
                allowfullscreen
                >
        </iframe>
    }
    else if (host == "youtube") {
        <iframe class="hosted-video__wrapper"
                width="560"
                height="315"
                src="https://www.youtube.com/embed/@(Model.VideoId)"
                frameborder="0"
                allowfullscreen
                >
        </iframe>
    }
}
```

## Structured Date and time model

`IDateTimeContent` provides a date and time value with the timezone:
* `DateTime?`
* `string`

By getting each value you can generate date and time with a timezone.

### Example

```csharp
    public partial class Article
    {
...
        public IDateTimeContent CreatedAt { get; set; }
...
    }
```

Replaces `DateTime?` type of a date time element.

## Modular content

`IContentItem` provides default content item system attributes : `IContentItemSystemAttributes`

Replaces `object` type of a modular content and subpages elements as well as every content item implements this interface.

## Other resources
- [How to Render Different Output for Rich Text in Kontent.ai Using the Delivery .NET SDK](https://robwest.info/articles/how-to-render-different-output-for-rich-text-in-kentico-kontent-using-the-delivery-net-sdk) by [Rob West
](https://github.com/robertgregorywest)
  - Rob describes how to render different output for AMP using ASP.NET Core Display Templates
