## Contents

<!-- TOC -->

- [Contents](#contents)
- [Content links](#content-links)
- [Implementing a resolver](#implementing-a-resolver)
- [Registering a resolver](#registering-a-resolver)
- [Retrieving Rich text content](#retrieving-rich-text-content)

<!-- /TOC -->

## Content links

[Rich text elements](https://docs.kontent.ai/tutorials/write-and-collaborate/write-content/composing-content-in-the-rich-text-editor#adding-links) in Kontent.ai can contain links to other content items. For example, if you run a blog, these content item links might represent hyperlinks to other blog posts or your contact page.

Without adjusting your application, any link in a Rich text element that points to a content item will contain an empty value.

```html
<p>Each AeroPress comes with a <a href="" data-item-id="65832c4e-8e9c-445f-a001-b9528d13dac8">pack of filters</a> included in the box.</p>
```

To make sure such links resolve correctly on your website, you need to complete these steps:

1. Implement a content link URL resolver.
2. Register the resolver via DI or through the `DeliveryClientBuilder`.
3. Retrieve content of a Rich text element.

## Implementing a resolver

Your resolver must implement the `IContentLinkUrlResolver` interface, which defines two methods  for resolving URLs to content items, `ResolveLinkUrlAsync` and `ResolveBrokenLinkUrlAsync`.

* **ResolveLinkUrlAsync** – used when the linked content item is available.
* **ResolveBrokenLinkUrlAsync** – used when the linked content item is not available.

When are content items available?

* For live environment, a content item is available when published, and unavailable when deleted or unpublished.
* For preview environment, a content item is available when it exists in the project, and unavailable when deleted.

```csharp
// Sample resolver implementation
public class CustomContentLinkUrlResolver : IContentLinkUrlResolver
{
    public Task<string> ResolveLinkUrlAsync(IContentLink link)
    {
        // Resolves URLs to content items based on the 'accessory' content type
        if (link.ContentTypeCodename == "accessory") {
            return Task.FromResult($"/accessories/{link.UrlSlug}");
        }

        // TBD: Add the rest of the resolver logic
    }

    public Task<string> ResolveBrokenLinkUrlAsync()
    {
        // Resolves URLs to unavailable content items
        return Task.FromResult("/404");
    }
}
```

When building the resolver logic, you can use the `link` parameter in your code.

The `link` parameter provides the following information about the linked content item:

Property | Description | Example
---------|-------------|--------
`Id` | The identifier of the linked content item. | `65832c4e-8e9c-445f-a001-b9528d13dac8`
`Codename` | The codename of the linked content item. | `aeropress_filters`
`UrlSlug` | The URL slug of the linked content item. The value is `null` if the item's content type doesn't have a URL slug element in its definition. | `aeropress-filters`
`ContentTypeCodename` | The content type codename of the linked content item. | `accessory`

## Registering a resolver

Once you implement the link resolver, you need to either register it within `IServiceCollection`

```csharp
// Registers the resolver in IServiceCollection
// or another framework you are using for dependency injection
services
    .AddSingleton<IContentLinkUrlResolver, CustomContentLinkUrlResolver>()
    .AddDeliveryClient(Configuration);
```

or within the `IDeliveryClient` instance through the `DeliveryClientBuilder` class

```csharp
// Sets the resolver as an optional dependency of the `IDeliveryClient` instance
IDeliveryClient client = DeliveryClientBuilder
    .WithProjectId("975bf280-fd91-488c-994c-2f04416e5ee3")
    .WithContentLinkUrlResolver(new CustomContentLinkUrlResolver())
    .Build();
```

## Retrieving Rich text content

Retrieve an item:

```csharp
// Retrieves the 'aeropress' content item
var brewer = await client.GetItemAsync<Brewer>("aeropress");

// Retrieves text from a rich-text element named 'long_description'
string description = brewer.LongDescription;
```

The URL to the content item in the rich-text element is now automatically resolved.

```html
<p>Each AeroPress comes with a <a href="/accessories/aeropress-filters" data-item-id="65832c4e-8e9c-445f-a001-b9528d13dac8">pack of filters</a> included in the box.</p>
```
