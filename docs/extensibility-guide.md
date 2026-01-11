# Extensibility Guide

This guide covers how to customize and extend the Kontent.ai Delivery SDK to fit your specific needs.

## Table of Contents

- [Overview](#overview)
- [Type Providers](#type-providers)
  - [ITypeProvider Interface](#itypeprovider-interface)
  - [Creating a Custom Type Provider](#creating-a-custom-type-provider)
  - [Registering Type Providers](#registering-type-providers)
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
    Type? TryGetModelType(string contentType);

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

    public Type? TryGetModelType(string contentType)
    {
        return _typeMap.TryGetValue(contentType, out var type) ? type : null;
    }

    public string? GetCodename(Type contentType)
    {
        return _codenameMap.TryGetValue(contentType, out var codename) ? codename : null;
    }
}
```

### Generated Type Provider

The [Kontent.ai Model Generator](https://github.com/kontent-ai/model-generator-net) automatically creates a type provider alongside your models:

```bash
dotnet tool install -g Kontent.Ai.ModelGenerator
KontentModelGenerator --environmentid <your-environment-id> --outputdir Models
```

This generates a `TypeProvider` class that maps all your content types to their corresponding models.

### Registering Type Providers

#### With Dependency Injection

```csharp
// Register your custom type provider
services.AddSingleton<ITypeProvider, CustomTypeProvider>();

// Or use the generated type provider
services.AddSingleton<ITypeProvider, GeneratedTypeProvider>();

// Then register the delivery client
services.AddDeliveryClient(options =>
{
    options.EnvironmentId = "your-environment-id";
});
```

#### Without Dependency Injection

```csharp
var client = DeliveryClientBuilder
    .WithEnvironmentId("your-environment-id")
    .WithTypeProvider(new CustomTypeProvider())
    .Build();
```

### How Type Resolution Works

When the SDK deserializes content items:

1. It reads the `system.type` property from the JSON
2. Calls `ITypeProvider.TryGetModelType(contentType)` to get the CLR type
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

### 1. Use Generated Type Providers

For most projects, use the model generator rather than creating type providers manually:

```bash
KontentModelGenerator --environmentid <your-env-id> --outputdir Models
```

This ensures your type mappings stay synchronized with your content types.

### 2. Keep Property Mappers Simple

Custom property mappers can introduce subtle bugs. Prefer using `[JsonPropertyName]` attributes for explicit mappings:

```csharp
public record Article
{
    [JsonPropertyName("body_copy")]
    public RichTextContent BodyCopy { get; init; }
}
```

### 3. Register Extensions Before the Client

Register your custom implementations before calling `AddDeliveryClient`:

```csharp
// ✅ Correct order
services.AddSingleton<ITypeProvider, CustomTypeProvider>();
services.AddDeliveryClient(options => { ... });

// ❌ Wrong order - custom provider may not be used
services.AddDeliveryClient(options => { ... });
services.AddSingleton<ITypeProvider, CustomTypeProvider>();
```

### 4. Test Custom Extensions

Write unit tests for custom type providers and property mappers:

```csharp
[Fact]
public void TryGetModelType_ReturnsCorrectType_ForArticle()
{
    var provider = new CustomTypeProvider();

    var result = provider.TryGetModelType("article");

    Assert.Equal(typeof(Article), result);
}

[Fact]
public void TryGetModelType_ReturnsNull_ForUnknownType()
{
    var provider = new CustomTypeProvider();

    var result = provider.TryGetModelType("unknown_type");

    Assert.Null(result);
}
```

---

**Related Documentation**:
- [Main README](../README.md)
- [Rich Text Customization](rich-text-customization.md)
- [Performance Optimization](performance-optimization.md)
