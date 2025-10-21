# Advanced Filtering Guide

This guide provides comprehensive coverage of the Kontent.ai Delivery SDK's filtering capabilities, enabling you to build complex queries for precise content retrieval.

## Table of Contents

- [Overview](#overview)
- [Property Paths](#property-paths)
  - [System Properties](#system-properties)
  - [Element Properties](#element-properties)
- [Filter Operators](#filter-operators)
  - [Equality Operators](#equality-operators)
  - [Comparison Operators](#comparison-operators)
  - [Range Operator](#range-operator)
  - [Array Operators](#array-operators)
  - [Text Search](#text-search)
  - [Multi-Value Operators](#multi-value-operators)
  - [Null and Empty Checks](#null-and-empty-checks)
- [Combining Filters](#combining-filters)
- [Advanced Query Patterns](#advanced-query-patterns)
- [Performance Considerations](#performance-considerations)
- [Troubleshooting](#troubleshooting)

## Overview

The SDK provides a type-safe filtering API through the `Filter()` method on query builders. All filters are combined with AND logic - items must match all specified filters to be returned.

```csharp
var result = await client.GetItems()
    .Filter(f => f.Equals(ItemSystemPath.Type, "article"))
    .Filter(f => f.GreaterThan(Elements.GetPath("rating"), 4.0))
    .Filter(f => f.Contains(Elements.GetPath("title"), "coffee"))
    .ExecuteAsync();
```

## Property Paths

### System Properties

System properties are accessed via the `ItemSystemPath` and `TypeSystemPath` classes:

#### Item System Properties

```csharp
using Kontent.Ai.Delivery.Abstractions;

// Content type
ItemSystemPath.Type           // "article", "product", etc.

// Codename
ItemSystemPath.Codename       // "homepage", "about_us", etc.

// Timestamps
ItemSystemPath.LastModified   // DateTime of last modification

// Language
ItemSystemPath.Language       // "en-US", "de-DE", etc.

// Collection
ItemSystemPath.Collection     // Collection codename

// Workflow
ItemSystemPath.WorkflowStep   // "published", "draft", etc.

// Identifiers
ItemSystemPath.Id             // GUID identifier
ItemSystemPath.Name           // Display name
```

#### Type System Properties

```csharp
TypeSystemPath.Codename       // Content type codename
TypeSystemPath.Name           // Content type name
TypeSystemPath.LastModified   // DateTime of last modification
```

### Element Properties

Element properties use the `Elements.GetPath()` helper:

```csharp
// Basic usage
Elements.GetPath("title")
Elements.GetPath("price")
Elements.GetPath("publish_date")

// Use in filters
.Filter(f => f.Equals(Elements.GetPath("category"), "news"))
```

**Note**: Element property names should match the codename defined in your Kontent.ai content type.

## Filter Operators

### Equality Operators

#### Equals

Match items where a property equals a specific value:

```csharp
// String equality
.Filter(f => f.Equals(ItemSystemPath.Type, "article"))
.Filter(f => f.Equals(Elements.GetPath("category"), "technology"))

// Numeric equality
.Filter(f => f.Equals(Elements.GetPath("rating"), 5.0))

// Date equality
.Filter(f => f.Equals(Elements.GetPath("publish_date"), new DateTime(2024, 1, 15)))

// GUID equality
.Filter(f => f.Equals(ItemSystemPath.Id, Guid.Parse("...")))
```

#### NotEquals

Match items where a property does not equal a specific value:

```csharp
.Filter(f => f.NotEquals(ItemSystemPath.Collection, "archived"))
.Filter(f => f.NotEquals(Elements.GetPath("status"), "draft"))
```

### Comparison Operators

Comparison operators work with numbers, dates, and strings:

#### GreaterThan

```csharp
// Numeric comparison
.Filter(f => f.GreaterThan(Elements.GetPath("price"), 100.0))
.Filter(f => f.GreaterThan(Elements.GetPath("rating"), 4.5))

// Date comparison
var cutoffDate = DateTime.Now.AddMonths(-6);
.Filter(f => f.GreaterThan(ItemSystemPath.LastModified, cutoffDate))

// String comparison (alphabetical)
.Filter(f => f.GreaterThan(Elements.GetPath("title"), "M"))  // Titles starting with N-Z
```

#### GreaterThanOrEqual

```csharp
.Filter(f => f.GreaterThanOrEqual(Elements.GetPath("min_order_quantity"), 10.0))
.Filter(f => f.GreaterThanOrEqual(Elements.GetPath("publish_date"), DateTime.Today))
```

#### LessThan

```csharp
.Filter(f => f.LessThan(Elements.GetPath("price"), 50.0))
.Filter(f => f.LessThan(ItemSystemPath.LastModified, DateTime.Now.AddYears(-1)))
```

#### LessThanOrEqual

```csharp
.Filter(f => f.LessThanOrEqual(Elements.GetPath("discount_percentage"), 25.0))
.Filter(f => f.LessThanOrEqual(Elements.GetPath("stock_quantity"), 10.0))
```

### Range Operator

The `Range` operator filters items with values between inclusive bounds:

```csharp
// Numeric range (price between $100 and $500)
.Filter(f => f.Range(Elements.GetPath("price"), (100.0, 500.0)))

// Date range (last 30 days)
var startDate = DateTime.Now.AddDays(-30);
var endDate = DateTime.Now;
.Filter(f => f.Range(ItemSystemPath.LastModified, (startDate, endDate)))

// Rating range (3.5 to 4.5 stars)
.Filter(f => f.Range(Elements.GetPath("rating"), (3.5, 4.5)))
```

**Complete Example: Find Products in Price Range**

```csharp
var result = await client.GetItems<Product>()
    .Filter(f => f.Equals(ItemSystemPath.Type, "product"))
    .Filter(f => f.Range(Elements.GetPath("price"), (100.0, 500.0)))
    .Filter(f => f.GreaterThanOrEqual(Elements.GetPath("rating"), 4.0))
    .OrderBy(Elements.GetPath("price"), ascending: true)
    .ExecuteAsync();
```

### Array Operators

#### In

Match items where a property value is in a specified array:

```csharp
// String array
.Filter(f => f.In(ItemSystemPath.Type, new[] { "article", "blog_post", "news" }))

// Numeric array
.Filter(f => f.In(Elements.GetPath("priority"), new[] { 1.0, 2.0, 3.0 }))

// Date array
var dates = new[] {
    new DateTime(2024, 1, 1),
    new DateTime(2024, 2, 1),
    new DateTime(2024, 3, 1)
};
.Filter(f => f.In(Elements.GetPath("publish_date"), dates))

// GUID array
var categoryIds = new[] {
    Guid.Parse("..."),
    Guid.Parse("...")
};
.Filter(f => f.In(ItemSystemPath.Collection, categoryIds))
```

#### NotIn

Exclude items where a property value is in a specified array:

```csharp
.Filter(f => f.NotIn(ItemSystemPath.Collection, new[] { "archived", "deleted", "draft" }))
.Filter(f => f.NotIn(Elements.GetPath("status"), new[] { "sold_out", "discontinued" }))
```

### Text Search

#### Contains

Search for a substring within text elements:

```csharp
// Case-insensitive substring search
.Filter(f => f.Contains(Elements.GetPath("title"), "coffee"))
.Filter(f => f.Contains(Elements.GetPath("description"), "organic"))

// Search across multiple items
var searchTerm = "sustainability";
var result = await client.GetItems()
    .Filter(f => f.Contains(Elements.GetPath("content"), searchTerm))
    .ExecuteAsync();
```

**Note**: The `Contains` operator performs case-insensitive substring matching. For full-text search capabilities, consider implementing a dedicated search solution like Azure Search or Algolia.

### Multi-Value Operators

These operators work with multi-value elements like taxonomy or multiple choice fields:

#### Any

Match items where at least one value in a multi-value element matches:

```csharp
// Taxonomy: items tagged with "featured" OR "trending"
.Filter(f => f.Any(Elements.GetPath("tags"), "featured", "trending"))

// Multiple choice: items with any of the specified categories
.Filter(f => f.Any(Elements.GetPath("categories"), "tech", "science", "innovation"))

// Modular content: items containing any of these components
.Filter(f => f.Any(Elements.GetPath("components"), "hero_banner", "video_section"))
```

#### All

Match items where ALL specified values are present in a multi-value element:

```csharp
// Taxonomy: items that have BOTH "featured" AND "trending" tags
.Filter(f => f.All(Elements.GetPath("tags"), "featured", "trending"))

// Multiple choice: items that belong to ALL specified categories
.Filter(f => f.All(Elements.GetPath("required_features"), "ssl", "backup", "monitoring"))
```

**Example: Featured Tech Articles**

```csharp
var result = await client.GetItems<Article>()
    .Filter(f => f.Equals(ItemSystemPath.Type, "article"))
    .Filter(f => f.All(Elements.GetPath("tags"), "featured", "tech"))
    .Filter(f => f.GreaterThan(Elements.GetPath("views"), 1000.0))
    .OrderBy(ItemSystemPath.LastModified, descending: true)
    .ExecuteAsync();
```

### Null and Empty Checks

#### Empty

Match items where an element has no value:

```csharp
// Find items without a description
.Filter(f => f.Empty(Elements.GetPath("description")))

// Find items without a featured image
.Filter(f => f.Empty(Elements.GetPath("featured_image")))

// Find items without tags
.Filter(f => f.Empty(Elements.GetPath("tags")))
```

#### NotEmpty

Match items where an element has a value:

```csharp
// Require items to have a description
.Filter(f => f.NotEmpty(Elements.GetPath("description")))

// Require items to have at least one author
.Filter(f => f.NotEmpty(Elements.GetPath("authors")))

// Ensure items have a publish date
.Filter(f => f.NotEmpty(Elements.GetPath("publish_date")))
```

## Combining Filters

All filters are combined with AND logic. Each `.Filter()` call adds another condition that must be satisfied:

### Basic Combination

```csharp
var result = await client.GetItems<Product>()
    // Must be a product
    .Filter(f => f.Equals(ItemSystemPath.Type, "product"))
    // Must be in stock
    .Filter(f => f.GreaterThan(Elements.GetPath("stock_quantity"), 0.0))
    // Must have a rating of 4 or higher
    .Filter(f => f.GreaterThanOrEqual(Elements.GetPath("rating"), 4.0))
    // Must be in the electronics or computers category
    .Filter(f => f.Any(Elements.GetPath("categories"), "electronics", "computers"))
    // Must not be discontinued
    .Filter(f => f.NotEquals(Elements.GetPath("status"), "discontinued"))
    .ExecuteAsync();
```

### Complex E-Commerce Query

```csharp
public async Task<IDeliveryResult<IEnumerable<Product>>> GetFeaturedProductsAsync(
    string[] categories,
    double minPrice,
    double maxPrice,
    double minRating)
{
    return await client.GetItems<Product>()
        .Filter(f => f.Equals(ItemSystemPath.Type, "product"))
        .Filter(f => f.Range(Elements.GetPath("price"), (minPrice, maxPrice)))
        .Filter(f => f.GreaterThanOrEqual(Elements.GetPath("rating"), minRating))
        .Filter(f => f.Any(Elements.GetPath("categories"), categories))
        .Filter(f => f.GreaterThan(Elements.GetPath("stock_quantity"), 0.0))
        .Filter(f => f.NotEmpty(Elements.GetPath("product_image")))
        .Filter(f => f.NotIn(Elements.GetPath("status"), new[] { "discontinued", "out_of_stock" }))
        .OrderBy(Elements.GetPath("featured_priority"), descending: true)
        .Limit(20)
        .ExecuteAsync();
}
```

### Content Discovery Query

```csharp
public async Task<IDeliveryResult<IEnumerable<Article>>> GetRecentArticlesAsync(
    string category,
    int daysBack = 30)
{
    var cutoffDate = DateTime.Now.AddDays(-daysBack);

    return await client.GetItems<Article>()
        .Filter(f => f.Equals(ItemSystemPath.Type, "article"))
        .Filter(f => f.Equals(Elements.GetPath("category"), category))
        .Filter(f => f.GreaterThan(ItemSystemPath.LastModified, cutoffDate))
        .Filter(f => f.NotEmpty(Elements.GetPath("featured_image")))
        .Filter(f => f.NotEmpty(Elements.GetPath("summary")))
        .Filter(f => f.Equals(ItemSystemPath.WorkflowStep, "published"))
        .OrderBy(ItemSystemPath.LastModified, descending: true)
        .WithTotalCount()
        .Limit(50)
        .ExecuteAsync();
}
```

## Advanced Query Patterns

### Dynamic Filter Building

Build filters dynamically based on user input:

```csharp
public async Task<IDeliveryResult<IEnumerable<IContentItem>>> SearchProductsAsync(
    ProductSearchCriteria criteria)
{
    var query = client.GetItems()
        .Filter(f => f.Equals(ItemSystemPath.Type, "product"));

    // Optional price range
    if (criteria.MinPrice.HasValue || criteria.MaxPrice.HasValue)
    {
        var min = criteria.MinPrice ?? 0;
        var max = criteria.MaxPrice ?? double.MaxValue;
        query = query.Filter(f => f.Range(Elements.GetPath("price"), (min, max)));
    }

    // Optional categories
    if (criteria.Categories?.Any() == true)
    {
        query = query.Filter(f => f.Any(Elements.GetPath("categories"), criteria.Categories));
    }

    // Optional minimum rating
    if (criteria.MinRating.HasValue)
    {
        query = query.Filter(f => f.GreaterThanOrEqual(
            Elements.GetPath("rating"),
            criteria.MinRating.Value));
    }

    // Optional text search
    if (!string.IsNullOrWhiteSpace(criteria.SearchText))
    {
        query = query.Filter(f => f.Contains(Elements.GetPath("title"), criteria.SearchText));
    }

    return await query
        .OrderBy(criteria.SortBy ?? Elements.GetPath("title"), criteria.Descending)
        .Limit(criteria.PageSize)
        .Skip(criteria.Page * criteria.PageSize)
        .WithTotalCount()
        .ExecuteAsync();
}
```

### Reusable Filter Fragments

Create reusable filter logic:

```csharp
public static class ContentFilters
{
    public static IItemsQuery<T> WithPublishedStatus<T>(this IItemsQuery<T> query)
    {
        return query
            .Filter(f => f.Equals(ItemSystemPath.WorkflowStep, "published"))
            .Filter(f => f.NotEquals(ItemSystemPath.Collection, "archived"));
    }

    public static IItemsQuery<T> WithImageRequired<T>(this IItemsQuery<T> query, string elementName)
    {
        return query.Filter(f => f.NotEmpty(Elements.GetPath(elementName)));
    }

    public static IItemsQuery<T> WithinLastDays<T>(this IItemsQuery<T> query, int days)
    {
        var cutoffDate = DateTime.Now.AddDays(-days);
        return query.Filter(f => f.GreaterThan(ItemSystemPath.LastModified, cutoffDate));
    }
}

// Usage
var result = await client.GetItems<Article>()
    .WithPublishedStatus()
    .WithImageRequired("featured_image")
    .WithinLastDays(7)
    .ExecuteAsync();
```

### Language and Collection Filtering

```csharp
// Get content from specific collection
var result = await client.GetItems()
    .Filter(f => f.Equals(ItemSystemPath.Collection, "marketing_content"))
    .WithLanguage("en-US")
    .ExecuteAsync();

// Get content from multiple collections
var collections = new[] { "blog", "news", "press_releases" };
var result = await client.GetItems()
    .Filter(f => f.In(ItemSystemPath.Collection, collections))
    .ExecuteAsync();
```

## Performance Considerations

### Indexing

- **System properties** are indexed and perform well in filters
- **Element properties** may have varying performance depending on content volume
- Use system properties when possible for better performance

### Query Optimization

1. **Filter First, Then Retrieve**: Apply filters to reduce the dataset before retrieving full content:

```csharp
// Good: Filter first
var result = await client.GetItems()
    .Filter(f => f.Equals(ItemSystemPath.Type, "article"))
    .Filter(f => f.GreaterThan(Elements.GetPath("rating"), 4.0))
    .Limit(10)
    .ExecuteAsync();

// Less efficient: Retrieving too much data
var result = await client.GetItems()
    .Limit(1000)  // Large dataset
    .ExecuteAsync();
```

2. **Use Pagination**: For large result sets, use `Skip()` and `Limit()`:

```csharp
var pageSize = 20;
var page = 0;

var result = await client.GetItems()
    .Filter(f => f.Equals(ItemSystemPath.Type, "article"))
    .OrderBy(ItemSystemPath.LastModified, descending: true)
    .Skip(page * pageSize)
    .Limit(pageSize)
    .WithTotalCount()
    .ExecuteAsync();
```

3. **Use Items Feed for Bulk Operations**: When processing all items, use `GetItemsFeed()`:

```csharp
var query = client.GetItemsFeed()
    .Filter(f => f.Equals(ItemSystemPath.Type, "article"));

await foreach (var item in query.ExecuteAsync())
{
    // Process each item efficiently
}
```

### Caching Strategy

Filters are part of the cache key. Be mindful of filter combinations:

```csharp
// These create different cache entries
.Filter(f => f.Range(Elements.GetPath("price"), (100.0, 500.0)))
.Filter(f => f.Range(Elements.GetPath("price"), (100.0, 600.0)))  // Different cache key
```

For frequently used filter combinations, consider:
- Pre-warming the cache on application startup
- Using consistent filter patterns
- Implementing custom cache key strategies if needed

### Rate Limiting

- Each query counts against your API rate limit
- Implement caching to reduce API calls
- Use broader filters to retrieve more items in fewer requests when appropriate

## Troubleshooting

### No Results Returned

**Problem**: Query returns empty results when you expect data.

**Solutions**:

1. **Check filter conditions**: Ensure all filters can be satisfied simultaneously
2. **Verify element codenames**: Element names must match exactly (case-sensitive)
3. **Check language variant**: Ensure content exists in the requested language
4. **Verify workflow status**: Content may not be published

```csharp
// Debug by removing filters one by one
var result = await client.GetItems()
    .Filter(f => f.Equals(ItemSystemPath.Type, "article"))
    // .Filter(f => f.GreaterThan(Elements.GetPath("rating"), 4.0))  // Try commenting out
    .ExecuteAsync();
```

### Filter Not Working as Expected

**Problem**: Filter appears to be ignored or produces unexpected results.

**Solutions**:

1. **Check data types**: Ensure you're using the correct type (string, double, DateTime)
2. **Verify element values**: Check actual values in Kontent.ai
3. **Check for typos**: Element codenames are case-sensitive

```csharp
// Wrong: Using string instead of double
.Filter(f => f.Equals(Elements.GetPath("price"), "100"))  // ❌

// Correct: Using double
.Filter(f => f.Equals(Elements.GetPath("price"), 100.0))  // ✅
```

### Performance Issues

**Problem**: Queries are slow.

**Solutions**:

1. **Add caching**: See [Caching Guide](caching-guide.md)
2. **Reduce result set**: Use tighter filters and smaller limits
3. **Optimize depth**: Don't retrieve more linked content levels than needed
4. **Use projection**: Only request needed elements with `WithElements()`

```csharp
var result = await client.GetItems<Article>()
    .Filter(f => f.Equals(ItemSystemPath.Type, "article"))
    .WithElements("title", "summary", "featured_image")  // Only retrieve needed elements
    .Depth(1)  // Don't retrieve deep linked content
    .Limit(10)
    .ExecuteAsync();
```

---

**Related Documentation**:
- [Main README](../README.md)
- [Performance Optimization Guide](performance-optimization.md)
- [Caching Guide](caching-guide.md)
