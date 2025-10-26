# Rich Text Customization Guide

Rich text elements in Kontent.ai contain structured content that needs to be resolved into HTML for display. This guide covers all aspects of customizing rich text resolution, from basic link handling to complex asynchronous resolvers.

## Table of Contents

- [Overview](#overview)
- [Basic Rich Text Resolution](#basic-rich-text-resolution)
- [HTML Resolver Builder](#html-resolver-builder)
- [Content Item Link Resolvers](#content-item-link-resolvers)
  - [Global Link Resolver](#global-link-resolver)
  - [Type-Specific Link Resolvers](#type-specific-link-resolvers)
  - [URL Pattern Resolver](#url-pattern-resolver)
  - [Tuple-Based Link Resolvers](#tuple-based-link-resolvers)
- [Embedded Content Resolvers](#embedded-content-resolvers)
  - [Type-Specific Content Resolvers](#type-specific-content-resolvers)
  - [Async Content Resolvers](#async-content-resolvers)
  - [Nested Content Resolution](#nested-content-resolution)
  - [Tuple-Based Content Resolvers](#tuple-based-content-resolvers)
- [Inline Image Resolvers](#inline-image-resolvers)
- [Custom HTML Node Resolvers](#custom-html-node-resolvers)
- [Resolution Context](#resolution-context)
- [Real-World Examples](#real-world-examples)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Overview

Rich text elements in Kontent.ai can contain:

- **Content item links**: Hyperlinks to other content items
- **Embedded content**: Components and linked items displayed inline
- **Inline images**: Images inserted within the text
- **Standard HTML**: Headings, paragraphs, lists, etc.

The SDK provides the `HtmlResolverBuilder` to customize how each of these elements is rendered.

## Basic Rich Text Resolution

### Default Resolution

The simplest way to render rich text:

```csharp
var result = await client.GetItem<Article>("my-article").ExecuteAsync();

if (result.IsSuccess)
{
    var article = result.Value;
    var html = await article.Elements.BodyCopy.ToHtmlAsync();
}
```

The default resolver renders:
- Content item links as plain text
- Embedded content as empty strings
- Inline images as standard `<img>` tags
- HTML elements as-is

### Custom Resolution

Create a custom resolver for full control:

```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentItemLinkResolver("article", (link, _) =>
        ValueTask.FromResult($"<a href=\"/articles/{link.Metadata?.UrlSlug}\">{link.Text}</a>"))
    .Build();

var html = await article.Elements.BodyCopy.ToHtmlAsync(resolver);
```

## HTML Resolver Builder

The `HtmlResolverBuilder` provides a fluent API for configuring resolution:

```csharp
var resolver = new HtmlResolverBuilder()
    // Content item link resolvers
    .WithContentItemLinkResolver(globalLinkResolver)
    .WithContentItemLinkResolver("article", articleLinkResolver)
    .WithContentItemLinkResolvers(linkResolverDictionary)

    // Embedded content resolvers
    .WithContentResolver("tweet", tweetResolver)
    .WithContentResolver("video", videoResolver)
    .WithContentResolvers(contentResolverDictionary)

    // Inline image resolver
    .WithInlineImageResolver(imageResolver)

    // Custom HTML node resolvers
    .WithHtmlNodeResolver("h1", h1Resolver)
    .WithHtmlNodeResolverForAttribute("data-custom", "value", customResolver)

    .Build();
```

## Content Item Link Resolvers

Content item links are hyperlinks in rich text that reference other content items.

### Global Link Resolver

A global resolver handles all content item links regardless of type:

```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentItemLinkResolver((link, resolveChildren) =>
    {
        // Fallback URL if metadata is not available
        var url = link.Metadata?.UrlSlug != null
            ? $"/content/{link.Metadata.UrlSlug}"
            : $"/content/{link.ItemId}";

        return ValueTask.FromResult($"<a href=\"{url}\">{link.Text}</a>");
    })
    .Build();
```

**Link Properties:**

```csharp
public interface IContentItemLink
{
    string Text { get; }           // Link text as authored
    Guid ItemId { get; }           // Referenced item's ID
    IContentItem? Metadata { get; } // Full metadata if available
}
```

### Type-Specific Link Resolvers

Different content types often need different URL patterns:

```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentItemLinkResolver("article", (link, _) =>
    {
        var slug = link.Metadata?.UrlSlug ?? link.ItemId.ToString();
        return ValueTask.FromResult($"<a href=\"/articles/{slug}\">{link.Text}</a>");
    })
    .WithContentItemLinkResolver("product", (link, _) =>
    {
        var slug = link.Metadata?.UrlSlug ?? link.ItemId.ToString();
        return ValueTask.FromResult($"<a href=\"/shop/products/{slug}\">{link.Text}</a>");
    })
    .WithContentItemLinkResolver("author", (link, _) =>
    {
        var codename = link.Metadata?.System.Codename ?? link.ItemId.ToString();
        return ValueTask.FromResult($"<a href=\"/about/team/{codename}\">{link.Text}</a>");
    })
    .Build();
```

**Priority**: Type-specific resolvers take precedence over global resolvers.

### URL Pattern Resolver

For simple routing patterns, use a URL pattern helper:

```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentItemLinkResolver(DefaultResolvers.UrlPatternResolver(
        new Dictionary<string, string>
        {
            ["article"] = "/articles/{urlslug}",
            ["blog_post"] = "/blog/{urlslug}",
            ["product"] = "/shop/products/{urlslug}",
            ["category"] = "/shop/categories/{codename}",
            ["author"] = "/about/team/{codename}"
        },
        fallbackPattern: "/content/{id}"))
    .Build();
```

**Pattern Placeholders:**
- `{urlslug}`: Uses `UrlSlug` property
- `{codename}`: Uses `System.Codename`
- `{id}`: Uses item GUID
- `{name}`: Uses `System.Name`

### Tuple-Based Link Resolvers

Pass multiple type-specific link resolvers using tuple overloads:

```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentItemLinkResolvers(
        ("article", (link, _) =>
            ValueTask.FromResult($"<a href=\"/articles/{link.Metadata?.UrlSlug}\">{link.Text}</a>")),
        ("product", (link, _) =>
            ValueTask.FromResult($"<a href=\"/shop/products/{link.Metadata?.UrlSlug}\">{link.Text}</a>")),
        ("author", (link, _) =>
            ValueTask.FromResult($"<a href=\"/about/team/{link.Metadata?.System.Codename}\">{link.Text}</a>"))
    )
    .Build();
```

### Advanced Link Resolution

Add custom attributes and classes:

```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentItemLinkResolver("article", (link, _) =>
    {
        var url = $"/articles/{link.Metadata?.UrlSlug}";
        var cssClass = link.Metadata?.Elements["featured"]?.ToString() == "true"
            ? "featured-link"
            : "standard-link";

        return ValueTask.FromResult(
            $"<a href=\"{url}\" class=\"{cssClass}\" data-item-id=\"{link.ItemId}\">{link.Text}</a>");
    })
    .Build();
```

## Embedded Content Resolvers

Embedded content (formerly inline content items) are components displayed within rich text.

### Type-Safe Content Resolvers (Recommended)

The SDK supports strongly-typed embedded content resolvers that provide compile-time type safety and IntelliSense:

```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentResolver<Tweet>(tweet =>
    {
        // Strongly-typed access to elements - no casting required!
        var tweetText = tweet.Elements.TweetText;
        var author = tweet.Elements.AuthorHandle;
        var tweetUrl = tweet.Elements.TweetUrl;

        return $@"
            <blockquote class=""twitter-tweet"">
                <p>{tweetText}</p>
                <cite>@{author}</cite>
                <a href=""{tweetUrl}"">View on Twitter</a>
            </blockquote>";
    })
    .WithContentResolver<Quote>(quote =>
    {
        var quoteText = quote.Elements.QuoteText;
        var attribution = quote.Elements.Attribution;

        return $@"
            <blockquote class=""pullquote"">
                <p>{quoteText}</p>
                {(attribution != null ? $"<cite>{attribution}</cite>" : "")}
            </blockquote>";
    })
    .Build();
```

**Benefits of Type-Safe Resolvers:**
- ✅ Compile-time type checking
- ✅ IntelliSense support for element properties
- ✅ Refactoring-friendly (rename detection)
- ✅ No runtime casting or null checks for element access

**Strongly-Typed Embedded Content Interface:**

```csharp
public interface IEmbeddedContent<out TModel> : IEmbeddedContent
    where TModel : IElementsModel
{
    TModel Elements { get; }  // Strongly-typed elements
}
```

### Pattern Matching with Embedded Content

Use pattern matching to filter and process specific embedded content types:

```csharp
// Process rich text blocks with pattern matching
foreach (var block in article.Elements.BodyCopy)
{
    switch (block)
    {
        case IEmbeddedContent<Tweet> tweet:
            Console.WriteLine($"Found tweet by @{tweet.Elements.AuthorHandle}");
            break;

        case IEmbeddedContent<Video> video:
            Console.WriteLine($"Found video: {video.Elements.Title}");
            break;

        case IEmbeddedContent<Quote> quote:
            Console.WriteLine($"Found quote: {quote.Elements.QuoteText}");
            break;
    }
}

// Or use LINQ extension methods
var allTweets = article.Elements.BodyCopy
    .GetEmbeddedContent<Tweet>()
    .ToList();

var allQuoteTexts = article.Elements.BodyCopy
    .GetEmbeddedElements<Quote>()
    .Select(q => q.QuoteText)
    .ToList();
```

### Codename-Based Content Resolvers (Legacy)

For scenarios where you don't have strongly-typed models, you can still use codename-based resolvers:

```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentResolver("tweet", content =>
    {
        // Requires manual element access and casting
        var tweetText = content.Elements["tweet_text"]?.ToString();
        var author = content.Elements["author_handle"]?.ToString();
        var tweetUrl = content.Elements["tweet_url"]?.ToString();

        return $@"
            <blockquote class=""twitter-tweet"">
                <p>{tweetText}</p>
                <cite>@{author}</cite>
                <a href=""{tweetUrl}"">View on Twitter</a>
            </blockquote>";
    })
    .Build();
```

**Resolver Priority:**
1. Type-based resolvers (highest priority)
2. Codename-based resolvers
3. Default/missing resolver handling

### Async Content Resolvers

Use async resolvers when you need to fetch additional data. Type-safe async resolvers provide the same benefits with `async`/`await`:

```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentResolver<HostedVideo>(async video =>
    {
        var videoId = video.Elements.VideoId;

        // Fetch video metadata from external API with full type safety
        var videoData = await _videoService.GetVideoDataAsync(videoId);

        return $@"
            <div class=""video-embed"" data-video-id=""{videoId}"">
                <iframe src=""https://youtube.com/embed/{videoId}""
                        title=""{videoData.Title}""
                        width=""560"" height=""315"">
                </iframe>
                <p class=""video-caption"">{videoData.Description}</p>
            </div>";
    })
    .WithContentResolver<ProductShowcase>(async showcase =>
    {
        var productReference = showcase.Elements.ProductReference;

        // Fetch real-time product data
        var product = await _productService.GetProductAsync(productReference.Id);

        return $@"
            <div class=""product-card"">
                <img src=""{product.ImageUrl}"" alt=""{product.Name}"" />
                <h3>{product.Name}</h3>
                <p class=""price"">${product.CurrentPrice:F2}</p>
                <p class=""stock"">{product.StockStatus}</p>
                <a href=""/products/{product.Id}"">View Details</a>
            </div>";
    })
    .Build();
```

### Nested Content Resolution

Handle embedded content that itself contains rich text:

```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentResolver("callout_box", async content =>
    {
        var title = content.Elements["title"]?.ToString();
        var bodyElement = content.Elements["body"] as RichTextElement;

        // Recursively resolve nested rich text
        var bodyHtml = bodyElement != null
            ? await bodyElement.ToHtmlAsync(resolver)  // Use the same resolver
            : "";

        return $@"
            <div class=""callout-box"">
                <h4>{title}</h4>
                <div class=""callout-body"">{bodyHtml}</div>
            </div>";
    })
    .Build();
```

### Tuple-Based Content Resolvers

Register multiple content resolvers using tuples for batch registration.

**Type-Safe Tuple Resolvers:**

```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentResolvers(
        (typeof(Tweet), content =>
            content is IEmbeddedContent<Tweet> tweet
                ? $"<div class=\"twitter-embed\"><a href=\"{tweet.Elements.Url}\">View Tweet</a></div>"
                : string.Empty),
        (typeof(Quote), content =>
            content is IEmbeddedContent<Quote> quote
                ? quote.Elements.Attribution != null
                    ? $"<blockquote><p>{quote.Elements.QuoteText}</p><cite>{quote.Elements.Attribution}</cite></blockquote>"
                    : $"<blockquote><p>{quote.Elements.QuoteText}</p></blockquote>"
                : string.Empty),
        (typeof(CodeSnippet), content =>
            content is IEmbeddedContent<CodeSnippet> snippet
                ? $"<pre><code class=\"language-{snippet.Elements.Language}\">{System.Web.HttpUtility.HtmlEncode(snippet.Elements.Code)}</code></pre>"
                : string.Empty)
    )
    .Build();
```

**Codename-Based Tuple Resolvers (Legacy):**

```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentResolvers(
        ("tweet", content =>
        {
            var url = content.Elements["url"]?.ToString();
            return $"<div class=\"twitter-embed\"><a href=\"{url}\">View Tweet</a></div>";
        }),
        ("quote", content =>
        {
            var text = content.Elements["quote_text"]?.ToString();
            var by = content.Elements["attribution"]?.ToString();
            return by != null
                ? $"<blockquote><p>{text}</p><cite>{by}</cite></blockquote>"
                : $"<blockquote><p>{text}</p></blockquote>";
        }),
        ("code_snippet", content =>
        {
            var code = content.Elements["code"]?.ToString();
            var lang = content.Elements["language"]?.ToString() ?? "plaintext";
            return $"<pre><code class=\"language-{lang}\">{System.Web.HttpUtility.HtmlEncode(code)}</code></pre>";
        })
    )
    .Build();
```

### Complex Component Example

```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentResolver("image_gallery", content =>
    {
        var imagesElement = content.Elements["images"] as IEnumerable<IContentItem>;
        if (imagesElement == null) return "";

        var imageHtml = string.Join("", imagesElement.Select(img =>
        {
            var url = img.Elements["image"]?.ToString();
            var caption = img.Elements["caption"]?.ToString();
            return $@"
                <figure class=""gallery-item"">
                    <img src=""{url}"" alt=""{caption}"" />
                    {(caption != null ? $"<figcaption>{caption}</figcaption>" : "")}
                </figure>";
        }));

        return $@"<div class=""image-gallery"">{imageHtml}</div>";
    })
    .Build();
```

## Inline Image Resolvers

Customize how images are rendered within rich text:

### Basic Image Resolution

```csharp
var resolver = new HtmlResolverBuilder()
    .WithInlineImageResolver((image, resolveChildren) =>
    {
        var url = image.Url;
        var description = image.Description ?? "Image";
        var width = image.Width;
        var height = image.Height;

        return ValueTask.FromResult(
            $"<img src=\"{url}\" alt=\"{description}\" width=\"{width}\" height=\"{height}\" />");
    })
    .Build();
```

**Image Properties:**

```csharp
public interface IInlineImage
{
    string Url { get; }
    string? Description { get; }
    int Width { get; }
    int Height { get; }
    Guid ImageId { get; }
}
```

### Responsive Images

Generate responsive image markup:

```csharp
var resolver = new HtmlResolverBuilder()
    .WithInlineImageResolver((image, _) =>
    {
        var baseUrl = image.Url;
        var alt = image.Description ?? "";

        // Generate srcset for different sizes
        var srcset = $@"
            {baseUrl}?w=320 320w,
            {baseUrl}?w=640 640w,
            {baseUrl}?w=1024 1024w";

        return ValueTask.FromResult($@"
            <img src=""{baseUrl}?w=640""
                 srcset=""{srcset}""
                 sizes=""(max-width: 640px) 100vw, 640px""
                 alt=""{alt}""
                 loading=""lazy"" />");
    })
    .Build();
```

### Images with Captions

Wrap images in figure elements:

```csharp
var resolver = new HtmlResolverBuilder()
    .WithInlineImageResolver((image, _) =>
    {
        var url = image.Url;
        var description = image.Description;

        var imgTag = $"<img src=\"{url}\" alt=\"{description ?? ""}\" loading=\"lazy\" />";

        return ValueTask.FromResult(
            description != null
                ? $"<figure><img src=\"{url}\" alt=\"{description}\" /><figcaption>{description}</figcaption></figure>"
                : imgTag);
    })
    .Build();
```

## Custom HTML Node Resolvers

Customize rendering of specific HTML elements:

### Element-Based Resolution

```csharp
var resolver = new HtmlResolverBuilder()
    .WithHtmlNodeResolver("h1", (node, resolveChildren) =>
    {
        var content = resolveChildren();
        var id = content.ToString()
            .ToLower()
            .Replace(" ", "-")
            .Replace("[^a-z0-9-]", "");

        return ValueTask.FromResult(
            $"<h1 id=\"{id}\" class=\"page-heading\">{content}</h1>");
    })
    .WithHtmlNodeResolver("h2", (node, resolveChildren) =>
    {
        var content = resolveChildren();
        return ValueTask.FromResult(
            $"<h2 class=\"section-heading\">{content}</h2>");
    })
    .Build();
```

### Attribute-Based Resolution

```csharp
var resolver = new HtmlResolverBuilder()
    .WithHtmlNodeResolverForAttribute("data-component", "code-snippet", (node, resolveChildren) =>
    {
        var code = resolveChildren();
        var language = node.GetAttribute("data-language") ?? "plaintext";

        return ValueTask.FromResult($@"
            <pre><code class=""language-{language}"">{code}</code></pre>");
    })
    .WithHtmlNodeResolverForAttribute("data-component", "alert", (node, resolveChildren) =>
    {
        var content = resolveChildren();
        var type = node.GetAttribute("data-type") ?? "info";

        return ValueTask.FromResult($@"
            <div class=""alert alert-{type}"" role=""alert"">
                {content}
            </div>");
    })
    .Build();
```

## Resolution Context

The `resolveChildren` delegate allows processing of child nodes:

```csharp
var resolver = new HtmlResolverBuilder()
    .WithContentItemLinkResolver((link, resolveChildren) =>
    {
        // Get the rendered children (link text with any nested formatting)
        var linkContent = resolveChildren();

        var url = $"/content/{link.Metadata?.UrlSlug}";

        return ValueTask.FromResult(
            $"<a href=\"{url}\" class=\"content-link\">{linkContent}</a>");
    })
    .WithHtmlNodeResolver("p", (node, resolveChildren) =>
    {
        // Process child nodes
        var content = resolveChildren();

        // Add custom class if paragraph is first in section
        var cssClass = node.PreviousSibling == null ? "first-paragraph" : "";

        return ValueTask.FromResult(
            $"<p class=\"{cssClass}\">{content}</p>");
    })
    .Build();
```

## Real-World Examples

### Blog Platform

```csharp
public IHtmlResolver CreateBlogResolver(string baseUrl)
{
    return new HtmlResolverBuilder()
        // Article links
        .WithContentItemLinkResolver("article", (link, _) =>
        {
            var slug = link.Metadata?.UrlSlug ?? link.ItemId.ToString();
            return ValueTask.FromResult($"<a href=\"{baseUrl}/articles/{slug}\">{link.Text}</a>");
        })
        // Author links
        .WithContentItemLinkResolver("author", (link, _) =>
        {
            var slug = link.Metadata?.UrlSlug ?? link.ItemId.ToString();
            return ValueTask.FromResult($"<a href=\"{baseUrl}/authors/{slug}\">{link.Text}</a>");
        })
        // Type-safe tweet embeds
        .WithContentResolver<Tweet>(tweet =>
        {
            var tweetUrl = tweet.Elements.Url;
            return $@"
                <div class=""twitter-embed"">
                    <blockquote class=""twitter-tweet"">
                        <a href=""{tweetUrl}"">View Tweet</a>
                    </blockquote>
                    <script async src=""https://platform.twitter.com/widgets.js""></script>
                </div>";
        })
        // Type-safe code snippets
        .WithContentResolver<CodeSnippet>(snippet =>
        {
            var code = snippet.Elements.Code;
            var language = snippet.Elements.Language ?? "plaintext";
            var caption = snippet.Elements.Caption;

            return $@"
                <figure class=""code-sample"">
                    <pre><code class=""language-{language}"">{System.Web.HttpUtility.HtmlEncode(code)}</code></pre>
                    {(caption != null ? $"<figcaption>{caption}</figcaption>" : "")}
                </figure>";
        })
        // Responsive images
        .WithInlineImageResolver((image, _) =>
        {
            var srcset = $"{image.Url}?w=320 320w, {image.Url}?w=640 640w, {image.Url}?w=1024 1024w";
            return ValueTask.FromResult($@"
                <img src=""{image.Url}?w=640""
                     srcset=""{srcset}""
                     sizes=""(max-width: 640px) 100vw, 640px""
                     alt=""{image.Description ?? ""}""
                     loading=""lazy"" />");
        })
        .Build();
}
```

### E-Commerce Platform

```csharp
public class ProductContentResolver
{
    private readonly IProductService _productService;

    public ProductContentResolver(IProductService productService)
    {
        _productService = productService;
    }

    public IHtmlResolver CreateResolver()
    {
        return new HtmlResolverBuilder()
            // Product links with real-time pricing
            .WithContentItemLinkResolver("product", async (link, _) =>
            {
                var productId = link.ItemId;
                var product = await _productService.GetProductAsync(productId);

                return $@"
                    <a href=""/products/{product.Slug}"" class=""product-link"">
                        {link.Text}
                        <span class=""price"">${product.CurrentPrice:F2}</span>
                    </a>";
            })
            // Type-safe product showcase component
            .WithContentResolver<ProductShowcase>(async showcase =>
            {
                var productRef = showcase.Elements.Product;
                if (productRef == null) return "";

                var productId = Guid.Parse(productRef.System.Id);
                var product = await _productService.GetProductAsync(productId);

                return $@"
                    <div class=""product-showcase"">
                        <img src=""{product.MainImage}?w=400"" alt=""{product.Name}"" />
                        <div class=""product-info"">
                            <h3>{product.Name}</h3>
                            <p class=""price"">${product.CurrentPrice:F2}</p>
                            {(product.OnSale ? $"<span class=\"sale-badge\">Sale!</span>" : "")}
                            <p class=""stock"">
                                {(product.InStock ? "In Stock" : "Out of Stock")}
                            </p>
                            <a href=""/products/{product.Slug}"" class=""btn btn-primary"">
                                View Product
                            </a>
                        </div>
                    </div>";
            })
            .Build();
    }
}
```

### Documentation Site

```csharp
public IHtmlResolver CreateDocumentationResolver()
{
    return new HtmlResolverBuilder()
        // Cross-reference links
        .WithContentItemLinkResolver("documentation_page", (link, _) =>
        {
            var slug = link.Metadata?.UrlSlug ?? link.ItemId.ToString();
            return ValueTask.FromResult(
                $"<a href=\"/docs/{slug}\" class=\"doc-link\">{link.Text}</a>");
        })
        // API reference links
        .WithContentItemLinkResolver("api_reference", (link, _) =>
        {
            var apiPath = link.Metadata?.Elements["api_path"]?.ToString();
            return ValueTask.FromResult(
                $"<a href=\"/api/{apiPath}\" class=\"api-link\"><code>{link.Text}</code></a>");
        })
        // Type-safe code examples
        .WithContentResolver<CodeExample>(example =>
        {
            var code = example.Elements.Code;
            var language = example.Elements.Language ?? "csharp";
            var title = example.Elements.Title;

            return $@"
                <div class=""code-example"">
                    {(title != null ? $"<div class=\"code-title\">{title}</div>" : "")}
                    <pre><code class=""language-{language}"">{System.Web.HttpUtility.HtmlEncode(code)}</code></pre>
                    <button class=""copy-button"" data-clipboard-text=""{System.Web.HttpUtility.HtmlEncode(code)}"">
                        Copy
                    </button>
                </div>";
        })
        // Type-safe callout boxes
        .WithContentResolver<Callout>(callout =>
        {
            var type = callout.Elements.Type ?? "info";
            var title = callout.Elements.Title;
            var bodyElement = callout.Elements.Body;

            var body = bodyElement?.ToHtmlAsync().Result ?? "";

            return $@"
                <div class=""callout callout-{type}"">
                    {(title != null ? $"<div class=\"callout-title\">{title}</div>" : "")}
                    <div class=""callout-body"">{body}</div>
                </div>";
        })
        // Heading anchors for table of contents
        .WithHtmlNodeResolver("h2", (node, resolveChildren) =>
        {
            var content = resolveChildren();
            var id = GenerateId(content.ToString());

            return ValueTask.FromResult($@"
                <h2 id=""{id}"">
                    <a href=""#{id}"" class=""heading-anchor"">#</a>
                    {content}
                </h2>");
        })
        .Build();
}

private string GenerateId(string text)
{
    return Regex.Replace(text.ToLower(), @"[^a-z0-9]+", "-").Trim('-');
}
```

## Best Practices

### 1. Use Type-Safe Resolvers

**Prefer type-safe resolvers over codename-based resolvers:**

```csharp
// ✅ Good: Type-safe with compile-time checking
.WithContentResolver<Quote>(quote =>
{
    return $"<blockquote>{quote.Elements.Text}</blockquote>";
})

// ❌ Avoid: Codename-based with runtime errors
.WithContentResolver("quote", content =>
{
    return $"<blockquote>{content.Elements["text"]}</blockquote>";
})
```

**Benefits:**
- Compile-time type safety prevents runtime errors
- IntelliSense support improves developer experience
- Refactoring tools work correctly with strongly-typed properties
- Better performance (no dictionary lookups for element access)

### 2. Performance Optimization

**Use Synchronous Resolvers When Possible:**

```csharp
// Good: Synchronous type-safe resolver
.WithContentResolver<Quote>(quote =>
{
    return $"<blockquote>{quote.Elements.Text}</blockquote>";
})

// Only use async when necessary
.WithContentResolver<ProductShowcase>(async showcase =>
{
    var data = await _externalService.GetDataAsync();  // Genuinely async
    return $"<div>{data}</div>";
})
```

**Cache Resolver Results:**

```csharp
private readonly IMemoryCache _cache;

.WithContentResolver("expensive_component", async content =>
{
    var cacheKey = $"component_{content.System.Id}";

    return await _cache.GetOrCreateAsync(cacheKey, async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
        return await GenerateExpensiveHtmlAsync(content);
    });
})
```

### 2. Security

**Always HTML-Encode User Content:**

```csharp
using System.Web;

.WithContentResolver("user_comment", content =>
{
    var rawText = content.Elements["comment"]?.ToString();
    var safeText = HttpUtility.HtmlEncode(rawText);

    return $"<div class=\"comment\">{safeText}</div>";
})
```

### 3. Maintainability

**Extract Resolvers to Methods:**

```csharp
public class RichTextResolvers
{
    // Type-safe resolver methods
    public static string ResolveTweet(IEmbeddedContent<Tweet> tweet)
    {
        var url = tweet.Elements.Url;
        return $"<blockquote class=\"twitter-tweet\"><a href=\"{url}\">Tweet</a></blockquote>";
    }

    public static string ResolveVideo(IEmbeddedContent<HostedVideo> video)
    {
        var videoId = video.Elements.VideoId;
        return $"<iframe src=\"https://youtube.com/embed/{videoId}\"></iframe>";
    }
}

// Usage with type-safe resolvers
var resolver = new HtmlResolverBuilder()
    .WithContentResolver<Tweet>(RichTextResolvers.ResolveTweet)
    .WithContentResolver<HostedVideo>(RichTextResolvers.ResolveVideo)
    .Build();
```

### 4. Testability

**Make Resolvers Testable:**

```csharp
public class HtmlResolverFactory
{
    private readonly IProductService _productService;
    private readonly IConfiguration _config;

    public HtmlResolverFactory(IProductService productService, IConfiguration config)
    {
        _productService = productService;
        _config = config;
    }

    public IHtmlResolver CreateResolver()
    {
        return new HtmlResolverBuilder()
            .WithContentResolver<ProductShowcase>(ResolveProductAsync)
            .Build();
    }

    // Type-safe testable method
    internal async Task<string> ResolveProductAsync(IEmbeddedContent<ProductShowcase> showcase)
    {
        var productId = Guid.Parse(showcase.Elements.Product.System.Id);
        var product = await _productService.GetProductAsync(productId);
        return $"<div>{product.Name}</div>";
    }
}

// In tests - easier to test with strongly-typed mocks
[Fact]
public async Task ResolveProductAsync_ReturnsCorrectHtml()
{
    var mockService = new Mock<IProductService>();
    var factory = new HtmlResolverFactory(mockService.Object, config);

    // Create strongly-typed test data
    var mockShowcase = CreateMockShowcase();

    var html = await factory.ResolveProductAsync(mockShowcase);

    Assert.Contains("Product Name", html);
}
```

## Troubleshooting

### Content Not Rendering

**Problem**: Embedded content appears as empty space.

**Solution**: Ensure you've registered a resolver for that content type:

```csharp
// Check what type is missing
var resolver = new HtmlResolverBuilder()
    .WithContentResolver("missing_type", content =>
    {
        // Temporary fallback to see what's missing
        return $"<!-- Missing resolver for: {content.System.Type} -->";
    })
    .Build();
```

### Links Not Working

**Problem**: Content item links render as plain text.

**Solution**: Register a link resolver:

```csharp
// At minimum, provide a global link resolver
var resolver = new HtmlResolverBuilder()
    .WithContentItemLinkResolver((link, _) =>
    {
        return ValueTask.FromResult($"<a href=\"/content/{link.ItemId}\">{link.Text}</a>");
    })
    .Build();
```

### Async Deadlocks

**Problem**: Application hangs when resolving rich text.

**Solution**: Always use `await` properly:

```csharp
// Wrong: Blocking async call
var html = article.Elements.BodyCopy.ToHtmlAsync(resolver).Result;  // ❌ Can deadlock

// Correct: Await properly
var html = await article.Elements.BodyCopy.ToHtmlAsync(resolver);  // ✅
```

### Performance Issues

**Problem**: Rich text resolution is slow.

**Solutions**:

1. **Minimize async resolvers**
2. **Cache external data fetches**
3. **Use `ValueTask` for synchronous paths**
4. **Consider pre-rendering for static content**

---

**Related Documentation**:
- [Main README](../README.md)
- [Performance Optimization Guide](performance-optimization.md)
- [Custom Type Converters](custom-type-converters.md)
