# Extensibility Guide

This guide covers how to customize and extend the Kontent.ai Delivery SDK to fit your specific needs.

## Table of Contents

- [Overview](#overview)
- [Type Providers](#type-providers)
  - [ITypeProvider Interface](#itypeprovider-interface)
  - [Source-Generated Type Provider (Recommended)](#source-generated-type-provider-recommended)
  - [Creating a Custom Type Provider](#creating-a-custom-type-provider)
  - [Registering Type Providers](#registering-type-providers)
  - [Auto-Discovery](#auto-discovery)
- [Property Mapping](#property-mapping)
  - [IPropertyMapper Interface](#ipropertymapper-interface)
  - [Creating a Custom Property Mapper](#creating-a-custom-property-mapper)
- [Rich Text Resolvers](#rich-text-resolvers)
- [Best Practices](#best-practices)

## Overview

The SDK provides several extension points for customizing behavior:

1. **Type Providers** - Map content type codenames to CLR types
2. **Property Mappers** - Customize how JSON properties map to model properties
3. **Rich Text Resolvers** - Control how rich text elements are rendered to HTML

## Type Providers

### ITypeProvider Interface

The `ITypeProvider` interface allows the SDK to resolve the correct CLR type for each content item based on its content type codename.

```csharp
public interface ITypeProvider
{
    /// <summary>
    /// Gets the CLR type for a content type codename.
    /// </summary>
    /// <param name="contentType">The content type codename.</param>
    /// <returns>The CLR type, or null if not found.</returns>
    Type? GetType(string contentType);

    /// <summary>
    /// Gets the content type codename for a CLR type.
    /// </summary>
    /// <param name="contentType">The CLR type.</param>
    /// <returns>The content type codename, or null if not found.</returns>
    string? GetCodename(Type contentType);
}
```

### Creating a Custom Type Provider

Create a custom type provider when you want to control which model types are used for different content types:

```csharp
public class CustomTypeProvider : ITypeProvider
{
    private readonly Dictionary<string, Type> _typeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["article"] = typeof(Article),
        ["product"] = typeof(Product),
        ["homepage"] = typeof(HomePage),
        ["author"] = typeof(Author),
        ["category"] = typeof(Category)
    };

    private readonly Dictionary<Type, string> _codenameMap;

    public CustomTypeProvider()
    {
        // Build reverse lookup
        _codenameMap = _typeMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    }

    public Type? GetType(string contentType)
    {
        return _typeMap.TryGetValue(contentType, out var type) ? type : null;
    }

    public string? GetCodename(Type contentType)
    {
        return _codenameMap.TryGetValue(contentType, out var codename) ? codename : null;
    }
}
```

### Source-Generated Type Provider (Recommended)

The SDK provides a Roslyn source generator that creates an `ITypeProvider` implementation at compile time. This approach offers several advantages:

- **Compile-time validation** - Duplicate codenames and invalid configurations are caught during build
- **Auto-discovery** - The SDK automatically finds the generated provider at runtime
- **Automatic type filtering** - Generic queries like `GetItems<Article>()` automatically add `system.type=article` filter
- **No runtime reflection** - Type mappings are generated as static dictionaries

#### Setup

Add the required NuGet packages:

```xml
<PackageReference Include="Kontent.Ai.Delivery.Attributes" Version="19.0.0-beta-6" />
<PackageReference Include="Kontent.Ai.Delivery.SourceGeneration" Version="19.0.0-beta-6"
    OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```

#### Usage

Decorate your model classes with the `[ContentTypeCodename]` attribute:

```csharp
using Kontent.Ai.Delivery.Attributes;

[ContentTypeCodename("article")]
public record Article
{
    public string Title { get; init; }
    public string Summary { get; init; }
    public RichTextContent BodyCopy { get; init; }
}

[ContentTypeCodename("product")]
public record Product
{
    public string Name { get; init; }
    public decimal Price { get; init; }
}

[ContentTypeCodename("author")]
public record Author
{
    public string Name { get; init; }
    public string Bio { get; init; }
}
```

The source generator produces a `GeneratedTypeProvider` class at compile time:

```csharp
// Auto-generated in: Kontent.Ai.Delivery.Generated namespace
public sealed class GeneratedTypeProvider : ITypeProvider
{
    private static readonly Dictionary<string, Type> _codenameToType =
        new(StringComparer.OrdinalIgnoreCase)
    {
        { "article", typeof(Article) },
        { "author", typeof(Author) },
        { "product", typeof(Product) },
    };

    private static readonly Dictionary<Type, string> _typeToCodename = new()
    {
        { typeof(Article), "article" },
        { typeof(Author), "author" },
        { typeof(Product), "product" },
    };

    public Type? GetModelType(string contentType)
        => _codenameToType.TryGetValue(contentType, out var type) ? type : null;

    public string? GetCodename(Type contentType)
        => _typeToCodename.TryGetValue(contentType, out var codename) ? codename : null;
}
```

#### Compile-Time Diagnostics

The source generator reports errors during compilation:

| ID | Severity | Description |
|----|----------|-------------|
| `KDSG001` | Error | Duplicate codename - two or more types have the same codename |
| `KDSG002` | Error | Invalid codename - null, empty, or whitespace |
| `KDSG003` | Error | Unsupported target - interfaces and abstract classes cannot be content types |

Example error:

```
error KDSG001: Duplicate codename 'article' used by: MyApp.Models.Article, MyApp.Models.BlogPost
```

#### Model Generator Alternative

The [Kontent.ai Model Generator](https://github.com/kontent-ai/model-generator-net) can also generate models with the `[ContentTypeCodename]` attribute:

```bash
dotnet tool install -g Kontent.Ai.ModelGenerator
KontentModelGenerator --environmentid <your-environment-id> --outputdir Models
```

When used with the source generation packages, the generated models automatically participate in compile-time type provider generation.

### Registering Type Providers

#### Auto-Discovery (Recommended)

When using source generation with `[ContentTypeCodename]` attributes, the SDK automatically discovers the `GeneratedTypeProvider` at runtime. No manual registration is needed:

```csharp
// Just register the delivery client - type provider is auto-discovered
services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
});
```

#### Explicit Registration with Dependency Injection

If you need to override auto-discovery or use a custom implementation, register your type provider **before** `AddDeliveryClient()`:

```csharp
// Register your custom type provider (takes precedence over auto-discovery)
services.AddSingleton<ITypeProvider, CustomTypeProvider>();

// Or explicitly register the generated type provider
services.AddSingleton<ITypeProvider, GeneratedTypeProvider>();

// Then register the delivery client
services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
});
```

The SDK uses `TryAddSingleton` internally, so your registration takes precedence.

#### Without Dependency Injection

```csharp
// Type provider is auto-discovered from source generation
using var container = DeliveryClientBuilder
    .WithOptions(builder => builder
        .WithEnvironmentId("your-environment-id")
        .Build())
    .Build();

// Or explicitly provide a type provider
using var container = DeliveryClientBuilder
    .WithOptions(builder => builder
        .WithEnvironmentId("your-environment-id")
        .Build())
    .WithTypeProvider(new CustomTypeProvider())
    .Build();
```

### Auto-Discovery

The SDK's default `TypeProvider` automatically discovers source-generated providers at runtime using this strategy:

1. **Entry assembly first** - Checks the application's entry assembly for `Kontent.Ai.Delivery.Generated.GeneratedTypeProvider`
2. **Referenced assemblies** - Checks assemblies referenced by the entry assembly
3. **Calling assembly fallback** - For test scenarios, checks the calling assembly

This bounded search is deterministic and avoids scanning the entire AppDomain. If multiple providers are found, the entry assembly's provider takes precedence.

```csharp
// The auto-discovery happens transparently when you use the SDK
var result = await client.GetItems<Article>().ExecuteAsync();
// ↑ SDK looks up "article" codename via auto-discovered GeneratedTypeProvider
//   and automatically adds system.type=article filter
```

### How Type Resolution Works

When the SDK deserializes content items:

1. It reads the `system.type` property from the JSON
2. Calls `ITypeProvider.GetType(contentType)` to get the CLR type
3. If a type is found, deserializes to that type
4. If no type is found, falls back to dynamic elements (`IDynamicElements`)

This enables:
- **Linked items** to be deserialized to their correct types
- **Embedded content** in rich text to use strongly-typed models
- **Pattern matching** on `IEmbeddedContent<T>` to work correctly

## Property Mapping

### IPropertyMapper Interface

The `IPropertyMapper` interface customizes how element codenames map to model properties:

```csharp
public interface IPropertyMapper
{
    /// <summary>
    /// Determines if a property matches a field name.
    /// </summary>
    /// <param name="modelProperty">The model property.</param>
    /// <param name="fieldName">The JSON field name.</param>
    /// <param name="contentType">The content type codename.</param>
    /// <returns>True if the property matches the field.</returns>
    bool IsMatch(PropertyInfo modelProperty, string fieldName, string contentType);
}
```

### Default Property Mapping

By default, the SDK uses these conventions:

1. **Exact match**: Property name matches element codename exactly
2. **Pascal case**: `url_slug` matches `UrlSlug` (underscores removed, pascal-cased)
3. **JsonPropertyName**: Uses `[JsonPropertyName("element_codename")]` attribute

```csharp
public record Article
{
    // Matches "title" element
    public string Title { get; init; }

    // Matches "url_slug" element (convention: underscores → pascal case)
    public string UrlSlug { get; init; }

    // Explicit mapping using JsonPropertyName
    [JsonPropertyName("body_copy")]
    public RichTextContent BodyCopy { get; init; }

    // Matches "related_articles" via convention
    public IEnumerable<IEmbeddedContent> RelatedArticles { get; init; }
}
```

### Creating a Custom Property Mapper

Create a custom mapper for special naming conventions:

```csharp
public class CustomPropertyMapper : IPropertyMapper
{
    public bool IsMatch(PropertyInfo modelProperty, string fieldName, string contentType)
    {
        // Check for JsonPropertyName attribute first
        var jsonAttr = modelProperty.GetCustomAttribute<JsonPropertyNameAttribute>();
        if (jsonAttr != null)
        {
            return string.Equals(jsonAttr.Name, fieldName, StringComparison.OrdinalIgnoreCase);
        }

        // Custom convention: prefix all elements with content type
        // e.g., "article_title" maps to Article.Title
        var expectedFieldName = $"{contentType}_{ToSnakeCase(modelProperty.Name)}";
        if (string.Equals(expectedFieldName, fieldName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Fall back to standard conventions
        var normalizedPropertyName = ToSnakeCase(modelProperty.Name);
        return string.Equals(normalizedPropertyName, fieldName, StringComparison.OrdinalIgnoreCase);
    }

    private static string ToSnakeCase(string input)
    {
        return string.Concat(
            input.Select((c, i) =>
                i > 0 && char.IsUpper(c) ? "_" + c : c.ToString())
        ).ToLowerInvariant();
    }
}
```

### Registering Property Mappers

```csharp
services.AddSingleton<IPropertyMapper, CustomPropertyMapper>();
```

## Rich Text Resolvers

For customizing how rich text content is rendered to HTML, see the [Rich Text Customization Guide](rich-text-customization.md).

Key extension points include:

- **Content resolvers** - Render embedded content items
- **Link resolvers** - Generate URLs for content item links
- **HTML node resolvers** - Transform specific HTML elements

## Best Practices

### 1. Use Source-Generated Type Providers

For most projects, use the `[ContentTypeCodename]` attribute with source generation rather than creating type providers manually:

```csharp
using Kontent.Ai.Delivery.Attributes;

[ContentTypeCodename("article")]
public record Article { /* ... */ }
```

This approach provides:
- **Compile-time validation** - Errors are caught during build, not at runtime
- **Auto-discovery** - No manual DI registration needed
- **Automatic type filtering** - Generic queries automatically filter by content type
- **Synchronization** - Type mappings are always in sync with your model definitions

If you use the [Kontent.ai Model Generator](https://github.com/kontent-ai/model-generator-net), it generates models with the `[ContentTypeCodename]` attribute automatically.

### 2. Keep Property Mappers Simple

Custom property mappers can introduce subtle bugs. Prefer using `[JsonPropertyName]` attributes for explicit mappings:

```csharp
public record Article
{
    [JsonPropertyName("body_copy")]
    public RichTextContent BodyCopy { get; init; }
}
```

### 3. Registration Order (When Not Using Auto-Discovery)

When using source generation, type provider registration is automatic. However, if you're registering custom implementations, register them **before** calling `AddDeliveryClient`:

```csharp
// ✅ Correct order (when overriding auto-discovery)
services.AddSingleton<ITypeProvider, CustomTypeProvider>();
services.AddDeliveryClient(options => { ... });

// ❌ Wrong order - custom provider may not be used
services.AddDeliveryClient(options => { ... });
services.AddSingleton<ITypeProvider, CustomTypeProvider>();
```

> [!NOTE]
> With source generation, you typically don't need to register the type provider at all - it's auto-discovered.

### 4. Test Custom Extensions

Write unit tests for custom type providers and property mappers:

```csharp
[Fact]
public void GetType_ReturnsCorrectType_ForArticle()
{
    var provider = new CustomTypeProvider();

    var result = provider.GetType("article");

    Assert.Equal(typeof(Article), result);
}

[Fact]
public void GetType_ReturnsNull_ForUnknownType()
{
    var provider = new CustomTypeProvider();

    var result = provider.GetType("unknown_type");

    Assert.Null(result);
}
```

---

**Related Documentation**:
- [Main README](../README.md)
- [Rich Text Customization](rich-text-customization.md)
- [Performance Optimization](performance-optimization.md)
