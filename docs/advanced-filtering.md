# Advanced Filtering Guide

This guide explains how to express Delivery API filtering using the SDK’s fluent filtering DSL.
The underlying API merges multiple filtering parameters using logical **AND** (more restrictive queries). See the Delivery API docs: [Filtering parameters](https://kontent.ai/learn/docs/apis/delivery-api/filtering-parameters).

## Overview

Filtering is added via `.Where(...)` on query builders. A single `.Where(...)` call can add **multiple** conditions:

```csharp
var result = await client.GetItems()
    .Where(f => f
        .System("type").IsEqualTo("article")
        // [contains] is for arrays (e.g. taxonomy, linked items, multiple choice), not strings.
        // See Delivery API docs: https://kontent.ai/learn/docs/apis/delivery-api/filtering-parameters?sl=1
        .Element("category").Contains("coffee")
        .System("last_modified").IsGreaterThan(DateTime.UtcNow.AddDays(-30)))
    .ExecuteAsync();
```

You can also chain multiple `.Where(...)` calls; all conditions are still ANDed together.

## Property paths

The DSL builds property paths for you:

- `System("<property>")` → `system.<property>`
  - Examples: `system.type`, `system.codename`, `system.language`, `system.last_modified`, `system.collection`, `system.workflow_step`
- `Element("<codename>")` → `elements.<codename>`
  - Examples: `elements.title`, `elements.price`, `elements.tags`, `elements.publish_date`

## Operator reference

### Equality

- `IsEqualTo(...)` → `[eq]`
- `IsNotEqualTo(...)` → `[neq]`

```csharp
// system.type[eq]=article
.Where(f => f.System("type").IsEqualTo("article"))

// elements.status[neq]=draft
.Where(f => f.Element("status").IsNotEqualTo("draft"))
```

### Comparison

- `IsLessThan(...)` → `[lt]`
- `IsLessThanOrEqualTo(...)` → `[lte]`
- `IsGreaterThan(...)` → `[gt]`
- `IsGreaterThanOrEqualTo(...)` → `[gte]`

```csharp
.Where(f => f.Element("price").IsGreaterThan(100.0))
.Where(f => f.System("last_modified").IsLessThanOrEqualTo(DateTime.UtcNow.AddDays(-7)))
```

### Range (inclusive)

- `IsWithinRange(lower, upper)` → `[range]` with `lower,upper`

```csharp
.Where(f => f.Element("price").IsWithinRange(100.0, 500.0))
.Where(f => f.System("last_modified").IsWithinRange(DateTime.Parse("2024-01-01"), DateTime.Parse("2024-06-30")))
```

### Collection membership

- `IsIn(...)` → `[in]`
- `IsNotIn(...)` → `[nin]`

```csharp
.Where(f => f.System("type").IsIn("article", "blog_post", "news"))
.Where(f => f.Element("rating").IsIn(4.0, 5.0))
```

### Arrays

- `Contains("...")` → `[contains]` (array contains a value)
- `ContainsAny(...)` → `[any]` (array contains at least one)
- `ContainsAll(...)` → `[all]` (array contains all)

```csharp
.Where(f => f.Element("category").Contains("coffee"))
.Where(f => f.Element("tags").ContainsAny("featured", "trending"))
.Where(f => f.Element("required_features").ContainsAll("warranty", "manual"))
```

### Empty checks

- `IsEmpty()` → `[empty]`
- `IsNotEmpty()` → `[nempty]`

```csharp
.Where(f => f.Element("seo_description").IsEmpty())
.Where(f => f.Element("summary").IsNotEmpty())
```

## Combining filters with other query parameters

```csharp
var result = await client.GetItems()
    .Where(f => f
        .System("type").IsEqualTo("article")
        .Element("tags").ContainsAny("beginner", "intermediate"))
    .WithLanguage("en-US")
    .WithElements("title", "summary")
    .Depth(2)
    .OrderBy("system.last_modified", ascending: false)
    .Limit(10)
    .ExecuteAsync();
```

## Language fallback control

Kontent.ai language fallbacks are controlled by how you combine the Delivery API `language` parameter with filtering on `system.language`.

- **Default behavior (fallbacks allowed)**: `.WithLanguage("es-ES")` requests the Spanish variant, and the API may fall back to the default language for items that are not translated.
- **Fallbacks disabled (only translated items)**: `.WithLanguage("es-ES", LanguageFallbackMode.Disabled)` automatically adds `system.language[eq]=es-ES` to the query, so only items actually translated into `es-ES` are returned.

Manual equivalent:

```csharp
var result = await client.GetItems()
    .WithLanguage("es-ES")
    .Where(f => f.System("language").IsEqualTo("es-ES"))
    .ExecuteAsync();
```

## Conditional composition (recommended)

Instead of building “filter objects”, use normal C# branching:

```csharp
var query = client.GetItems();

if (onlyArticles)
{
    query = query.Where(f => f.System("type").IsEqualTo("article"));
}

if (!string.IsNullOrWhiteSpace(searchText))
{
    // Note: Delivery API doesn't support substring contains on strings.
    // Use [contains]/[any]/[all] only for arrays (taxonomy, linked items, multiple choice).
    query = query.Where(f => f.Element("category").Contains(searchText));
}

var result = await query.ExecuteAsync();
```

## Troubleshooting

- **Unexpected no-results**: remember filters are ANDed; temporarily comment out filters to isolate the restrictive one.
- **Special characters**: string values are URL-encoded automatically (e.g. `&` becomes `%26`).
- **Date filtering**: the Delivery API compares date-time strings; prefer ISO-like inputs and be explicit about bounds (see Delivery docs: [Filtering parameters](https://kontent.ai/learn/docs/apis/delivery-api/filtering-parameters)).


