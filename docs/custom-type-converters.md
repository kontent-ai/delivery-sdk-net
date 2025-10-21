# Custom Type Converters Guide

The SDK provides extension points for customizing how content element values are converted to your model properties. This guide covers implementing custom type converters for specialized scenarios.

## Table of Contents

- [Overview](#overview)
- [When to Use Custom Converters](#when-to-use-custom-converters)
- [IElementValueConverter Interface](#ielementvalueconverter-interface)
- [Implementing Custom Converters](#implementing-custom-converters)
  - [Simple Converters](#simple-converters)
  - [Async Converters](#async-converters)
  - [Context-Aware Converters](#context-aware-converters)
- [Registering Converters](#registering-converters)
- [Common Scenarios](#common-scenarios)
  - [Custom Element Types](#custom-element-types)
  - [Value Transformations](#value-transformations)
  - [Complex JSON Parsing](#complex-json-parsing)
  - [External Data Enrichment](#external-data-enrichment)
- [Best Practices](#best-practices)
- [Testing Converters](#testing-converters)
- [Troubleshooting](#troubleshooting)

## Overview

The SDK uses converters to transform element values from the Delivery API JSON into your model properties. While the SDK includes built-in converters for standard elements (text, rich text, numbers, dates, taxonomies, etc.), you can create custom converters for:

- Custom element types
- Specialized value transformations
- Integration with external systems
- Complex business logic during deserialization

## When to Use Custom Converters

Use custom converters when:

✅ **Custom Elements**: You use custom elements in Kontent.ai
✅ **Value Transformation**: You need to transform values during deserialization
✅ **Complex Parsing**: Element values contain complex JSON that needs special handling
✅ **External Enrichment**: You need to fetch additional data from external sources
✅ **Business Logic**: Deserialization requires business rules or calculations

Don't use custom converters when:

❌ Simple mapping works fine
❌ Transformation can be done in your application layer
❌ You just need different property names (use `[JsonProperty]` instead)

## IElementValueConverter Interface

The converter interface is generic and type-safe:

```csharp
public interface IElementValueConverter<in T, TResult>
{
    Task<TResult?> ConvertAsync<TElement>(
        TElement element,
        ResolvingContext context)
        where TElement : IContentElementValue<T>;
}
```

**Type Parameters:**
- `T`: The input element value type (e.g., `string`, `decimal`, custom type)
- `TResult`: The output property type in your model

**Parameters:**
- `element`: The content element value from the API
- `context`: Resolution context with additional information

**Returns:**
- The converted value of type `TResult`, or `null`

## Implementing Custom Converters

### Simple Converters

A basic converter that transforms a value:

```csharp
using Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Converts a comma-separated string to a list of tags
/// </summary>
public class TagsConverter : IElementValueConverter<string, List<string>>
{
    public Task<List<string>?> ConvertAsync<TElement>(
        TElement element,
        ResolvingContext context)
        where TElement : IContentElementValue<string>
    {
        var value = element.Value;

        if (string.IsNullOrWhiteSpace(value))
            return Task.FromResult<List<string>?>(null);

        var tags = value
            .Split(',')
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();

        return Task.FromResult<List<string>?>(tags);
    }
}
```

**Usage in Model:**

```csharp
public class Article
{
    [JsonProperty("tags")]
    [ValueConverter(typeof(TagsConverter))]
    public List<string> Tags { get; set; }
}
```

### Async Converters

Converters that need to perform async operations:

```csharp
/// <summary>
/// Fetches author details from external API based on author ID
/// </summary>
public class AuthorEnrichmentConverter : IElementValueConverter<string, AuthorDetails>
{
    private readonly IAuthorService _authorService;
    private readonly ILogger<AuthorEnrichmentConverter> _logger;

    public AuthorEnrichmentConverter(
        IAuthorService authorService,
        ILogger<AuthorEnrichmentConverter> logger)
    {
        _authorService = authorService;
        _logger = logger;
    }

    public async Task<AuthorDetails?> ConvertAsync<TElement>(
        TElement element,
        ResolvingContext context)
        where TElement : IContentElementValue<string>
    {
        var authorId = element.Value;

        if (string.IsNullOrEmpty(authorId))
            return null;

        try
        {
            return await _authorService.GetAuthorDetailsAsync(authorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch author details for ID: {AuthorId}", authorId);
            return null;
        }
    }
}

public class AuthorDetails
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Bio { get; set; }
}
```

### Context-Aware Converters

Use the `ResolvingContext` for additional information:

```csharp
/// <summary>
/// Converts prices based on the content language/region
/// </summary>
public class LocalizedPriceConverter : IElementValueConverter<decimal, LocalizedPrice>
{
    private readonly ICurrencyService _currencyService;

    public LocalizedPriceConverter(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    public async Task<LocalizedPrice?> ConvertAsync<TElement>(
        TElement element,
        ResolvingContext context)
        where TElement : IContentElementValue<decimal>
    {
        var basePrice = element.Value;

        // Get language from context
        var language = context.Language ?? "en-US";
        var currency = GetCurrencyForLanguage(language);

        // Convert price to local currency
        var convertedAmount = await _currencyService.ConvertAsync(
            basePrice,
            "USD",
            currency);

        return new LocalizedPrice
        {
            Amount = convertedAmount,
            Currency = currency,
            FormattedPrice = FormatPrice(convertedAmount, currency, language)
        };
    }

    private string GetCurrencyForLanguage(string language) => language switch
    {
        "en-US" => "USD",
        "en-GB" => "GBP",
        "de-DE" => "EUR",
        "ja-JP" => "JPY",
        _ => "USD"
    };

    private string FormatPrice(decimal amount, string currency, string language)
    {
        return amount.ToString("C", CultureInfo.GetCultureInfo(language));
    }
}

public class LocalizedPrice
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string FormattedPrice { get; set; }
}
```

## Registering Converters

### Property-Level Registration

Use the `[ValueConverter]` attribute:

```csharp
public class Product
{
    [ValueConverter(typeof(TagsConverter))]
    public List<string> Tags { get; set; }

    [ValueConverter(typeof(LocalizedPriceConverter))]
    public LocalizedPrice Price { get; set; }
}
```

### Global Registration

Register converters in the DI container:

```csharp
services.AddSingleton<IElementValueConverter<string, List<string>>, TagsConverter>();
services.AddSingleton<IElementValueConverter<decimal, LocalizedPrice>, LocalizedPriceConverter>();

// With dependencies
services.AddSingleton<IAuthorService, AuthorService>();
services.AddSingleton<AuthorEnrichmentConverter>();
```

## Common Scenarios

### Custom Element Types

Handle custom elements from Kontent.ai:

```csharp
/// <summary>
/// Converts a color picker custom element to a Color object
/// </summary>
public class ColorPickerConverter : IElementValueConverter<string, Color>
{
    public Task<Color?> ConvertAsync<TElement>(
        TElement element,
        ResolvingContext context)
        where TElement : IContentElementValue<string>
    {
        var hexColor = element.Value;

        if (string.IsNullOrWhiteSpace(hexColor))
            return Task.FromResult<Color?>(null);

        // Remove # if present
        hexColor = hexColor.TrimStart('#');

        // Parse hex color (e.g., "FF5733")
        if (hexColor.Length == 6)
        {
            var r = Convert.ToByte(hexColor.Substring(0, 2), 16);
            var g = Convert.ToByte(hexColor.Substring(2, 2), 16);
            var b = Convert.ToByte(hexColor.Substring(4, 2), 16);

            return Task.FromResult<Color?>(new Color(r, g, b));
        }

        return Task.FromResult<Color?>(null);
    }
}

public class Color
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }

    public Color(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }

    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";
    public string ToRgb() => $"rgb({R}, {G}, {B})";
}
```

### Value Transformations

Transform values during deserialization:

```csharp
/// <summary>
/// Converts Markdown to HTML
/// </summary>
public class MarkdownToHtmlConverter : IElementValueConverter<string, HtmlString>
{
    private readonly IMarkdownService _markdownService;

    public MarkdownToHtmlConverter(IMarkdownService markdownService)
    {
        _markdownService = markdownService;
    }

    public Task<HtmlString?> ConvertAsync<TElement>(
        TElement element,
        ResolvingContext context)
        where TElement : IContentElementValue<string>
    {
        var markdown = element.Value;

        if (string.IsNullOrWhiteSpace(markdown))
            return Task.FromResult<HtmlString?>(null);

        var html = _markdownService.ToHtml(markdown);
        return Task.FromResult<HtmlString?>(new HtmlString(html));
    }
}

// Usage
public class BlogPost
{
    [ValueConverter(typeof(MarkdownToHtmlConverter))]
    public HtmlString Summary { get; set; }
}
```

### Complex JSON Parsing

Parse structured JSON from text elements:

```csharp
/// <summary>
/// Parses JSON configuration from a text element
/// </summary>
public class JsonConfigConverter : IElementValueConverter<string, AppConfiguration>
{
    public Task<AppConfiguration?> ConvertAsync<TElement>(
        TElement element,
        ResolvingContext context)
        where TElement : IContentElementValue<string>
    {
        var json = element.Value;

        if (string.IsNullOrWhiteSpace(json))
            return Task.FromResult<AppConfiguration?>(null);

        try
        {
            var config = JsonSerializer.Deserialize<AppConfiguration>(json);
            return Task.FromResult(config);
        }
        catch (JsonException ex)
        {
            // Log error and return default
            return Task.FromResult<AppConfiguration?>(new AppConfiguration());
        }
    }
}

public class AppConfiguration
{
    public Dictionary<string, string> Settings { get; set; } = new();
    public List<string> Features { get; set; } = new();
    public bool MaintenanceMode { get; set; }
}
```

### External Data Enrichment

Enrich content with external data:

```csharp
/// <summary>
/// Enriches product data with real-time inventory from external system
/// </summary>
public class ProductInventoryConverter : IElementValueConverter<string, ProductInventory>
{
    private readonly IInventoryService _inventoryService;
    private readonly IMemoryCache _cache;

    public ProductInventoryConverter(
        IInventoryService inventoryService,
        IMemoryCache cache)
    {
        _inventoryService = inventoryService;
        _cache = cache;
    }

    public async Task<ProductInventory?> ConvertAsync<TElement>(
        TElement element,
        ResolvingContext context)
        where TElement : IContentElementValue<string>
    {
        var productSku = element.Value;

        if (string.IsNullOrEmpty(productSku))
            return null;

        // Cache inventory data for 5 minutes
        var cacheKey = $"inventory_{productSku}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            try
            {
                return await _inventoryService.GetInventoryAsync(productSku);
            }
            catch (Exception)
            {
                // Return default on error
                return new ProductInventory
                {
                    Sku = productSku,
                    Available = false,
                    Quantity = 0
                };
            }
        });
    }
}

public class ProductInventory
{
    public string Sku { get; set; }
    public bool Available { get; set; }
    public int Quantity { get; set; }
    public DateTime LastUpdated { get; set; }
}
```

### Geocoding Converter

Convert address strings to coordinates:

```csharp
/// <summary>
/// Converts an address to geographic coordinates
/// </summary>
public class GeocodingConverter : IElementValueConverter<string, GeoLocation>
{
    private readonly IGeocodingService _geocodingService;

    public async Task<GeoLocation?> ConvertAsync<TElement>(
        TElement element,
        ResolvingContext context)
        where TElement : IContentElementValue<string>
    {
        var address = element.Value;

        if (string.IsNullOrWhiteSpace(address))
            return null;

        try
        {
            return await _geocodingService.GeocodeAsync(address);
        }
        catch (GeocodingException ex)
        {
            return new GeoLocation { Address = address, IsValid = false };
        }
    }
}

public class GeoLocation
{
    public string Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsValid { get; set; }
}
```

## Best Practices

### 1. Keep Converters Focused

Each converter should have a single responsibility:

```csharp
// Good: Single purpose
public class EmailNormalizationConverter : IElementValueConverter<string, string>
{
    public Task<string?> ConvertAsync<TElement>(TElement element, ResolvingContext context)
    {
        var email = element.Value?.Trim().ToLowerInvariant();
        return Task.FromResult(email);
    }
}

// Bad: Multiple responsibilities
public class DataConverter : IElementValueConverter<string, ComplexData>
{
    // Does too many things: parsing, validation, external calls, caching...
}
```

### 2. Handle Null Values

Always handle null or empty input gracefully:

```csharp
public async Task<TResult?> ConvertAsync<TElement>(...)
{
    if (string.IsNullOrWhiteSpace(element.Value))
        return null;  // or default value

    // Process non-null value
}
```

### 3. Use Async Only When Necessary

Use synchronous patterns when possible:

```csharp
// Good: Synchronous when no async work needed
public Task<List<string>?> ConvertAsync<TElement>(...)
{
    var result = element.Value.Split(',').ToList();
    return Task.FromResult<List<string>?>(result);
}

// Better: Use ValueTask for synchronous paths
public ValueTask<List<string>?> ConvertAsync<TElement>(...)
{
    var result = element.Value.Split(',').ToList();
    return new ValueTask<List<string>?>(result);
}
```

### 4. Cache External Data

Cache expensive operations:

```csharp
public class CachedExternalDataConverter : IElementValueConverter<string, ExternalData>
{
    private readonly IMemoryCache _cache;

    public async Task<ExternalData?> ConvertAsync<TElement>(...)
    {
        var id = element.Value;
        var cacheKey = $"external_{id}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return await FetchExternalDataAsync(id);
        });
    }
}
```

### 5. Log Errors

Log conversion failures for diagnostics:

```csharp
public class RobustConverter : IElementValueConverter<string, ParsedData>
{
    private readonly ILogger<RobustConverter> _logger;

    public async Task<ParsedData?> ConvertAsync<TElement>(...)
    {
        try
        {
            return await ParseDataAsync(element.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Conversion failed for element value: {Value}",
                element.Value);
            return null;  // or default value
        }
    }
}
```

### 6. Make Converters Testable

Design for testability:

```csharp
public class TestableConverter : IElementValueConverter<string, Result>
{
    private readonly IExternalService _service;

    // Constructor injection for easy mocking
    public TestableConverter(IExternalService service)
    {
        _service = service;
    }

    public async Task<Result?> ConvertAsync<TElement>(...)
    {
        return await _service.ProcessAsync(element.Value);
    }
}
```

## Testing Converters

### Unit Testing

```csharp
using Xunit;
using Moq;

public class TagsConverterTests
{
    [Fact]
    public async Task ConvertAsync_WithCommaSeparatedTags_ReturnsList()
    {
        // Arrange
        var converter = new TagsConverter();
        var elementMock = new Mock<IContentElementValue<string>>();
        elementMock.Setup(e => e.Value).Returns("tag1, tag2, tag3");

        // Act
        var result = await converter.ConvertAsync(elementMock.Object, new ResolvingContext());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains("tag1", result);
        Assert.Contains("tag2", result);
        Assert.Contains("tag3", result);
    }

    [Fact]
    public async Task ConvertAsync_WithEmptyString_ReturnsNull()
    {
        // Arrange
        var converter = new TagsConverter();
        var elementMock = new Mock<IContentElementValue<string>>();
        elementMock.Setup(e => e.Value).Returns("");

        // Act
        var result = await converter.ConvertAsync(elementMock.Object, new ResolvingContext());

        // Assert
        Assert.Null(result);
    }
}
```

### Integration Testing

```csharp
public class ConverterIntegrationTests
{
    [Fact]
    public async Task Converter_WorksWithRealDeliveryClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IElementValueConverter<string, List<string>>, TagsConverter>();
        services.AddDeliveryClient(options =>
        {
            options.EnvironmentId = "test-environment-id";
        });

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<IDeliveryClient>();

        // Act
        var result = await client.GetItem<Article>("test-article").ExecuteAsync();

        // Assert
        Assert.NotNull(result.Value.Tags);
    }
}
```

## Troubleshooting

### Converter Not Being Called

**Problem**: Custom converter doesn't seem to execute.

**Solutions**:

1. **Verify attribute placement**:
```csharp
// Correct
[ValueConverter(typeof(MyConverter))]
public MyType Property { get; set; }
```

2. **Check converter registration** in DI container
3. **Ensure type parameters match** exactly

### Null Values

**Problem**: Converter receives null unexpectedly.

**Solution**: Check element exists in content and has a value:

```csharp
public async Task<T?> ConvertAsync<TElement>(...)
{
    if (element?.Value == null)
    {
        _logger.LogWarning("Element value is null");
        return default;
    }

    // Process value
}
```

### Performance Issues

**Problem**: Conversion is slow.

**Solutions**:

1. **Cache expensive operations**
2. **Use async appropriately**
3. **Batch external API calls** if possible
4. **Profile converter execution** time

---

**Related Documentation**:
- [Main README](../README.md)
- [Performance Optimization Guide](performance-optimization.md)
- [Rich Text Customization](rich-text-customization.md)
