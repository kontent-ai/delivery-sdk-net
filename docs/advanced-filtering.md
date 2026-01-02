# Advanced Filtering Guide

This guide explains how to express Delivery API filtering using the SDK’s fluent filtering DSL.
The underlying API merges multiple filtering parameters using logical **AND** (more restrictive queries). See the Delivery API docs: [Filtering parameters](https://kontent.ai/learn/docs/apis/delivery-api/filtering-parameters).

## Overview

Filtering is added via `.Filter(...)` on query builders. A single `.Filter(...)` call can add **multiple** conditions:

```csharp
var result = await client.GetItems()
    .Filter(f => f
        .System("type").Eq("article")
        // [contains] is for arrays (e.g. taxonomy, linked items, multiple choice), not strings.
        // See Delivery API docs: https://kontent.ai/learn/docs/apis/delivery-api/filtering-parameters?sl=1
        .Element("category").Contains("coffee")
        .System("last_modified").Gt(DateTime.UtcNow.AddDays(-30)))
    .ExecuteAsync();
```

You can also chain multiple `.Filter(...)` calls; all conditions are still ANDed together.

## Property paths

The DSL builds property paths for you:

- `System("<property>")` → `system.<property>`
  - Examples: `system.type`, `system.codename`, `system.language`, `system.last_modified`, `system.collection`, `system.workflow_step`
- `Element("<codename>")` → `elements.<codename>`
  - Examples: `elements.title`, `elements.price`, `elements.tags`, `elements.publish_date`

## Operator reference

### Equality

- `Eq(...)` → `[eq]`
- `Neq(...)` → `[neq]`

```csharp
// system.type[eq]=article
.Filter(f => f.System("type").Eq("article"))

// elements.status[neq]=draft
.Filter(f => f.Element("status").Neq("draft"))
```

### Comparison

- `Lt(...)` → `[lt]`
- `Lte(...)` → `[lte]`
- `Gt(...)` → `[gt]`
- `Gte(...)` → `[gte]`

```csharp
.Filter(f => f.Element("price").Gt(100.0))
.Filter(f => f.System("last_modified").Lte(DateTime.UtcNow.AddDays(-7)))
```

### Range (inclusive)

- `Range(lower, upper)` → `[range]` with `lower,upper`

```csharp
.Filter(f => f.Element("price").Range(100.0, 500.0))
.Filter(f => f.System("last_modified").Range(DateTime.Parse("2024-01-01"), DateTime.Parse("2024-06-30")))
```

### Collection membership

- `In(...)` → `[in]`
- `Nin(...)` → `[nin]`

```csharp
.Filter(f => f.System("type").In("article", "blog_post", "news"))
.Filter(f => f.Element("rating").In(4.0, 5.0))
```

### Arrays

- `Contains("...")` → `[contains]` (array contains a value)
- `Any(...)` → `[any]` (array contains at least one)
- `All(...)` → `[all]` (array contains all)

```csharp
.Filter(f => f.Element("category").Contains("coffee"))
.Filter(f => f.Element("tags").Any("featured", "trending"))
.Filter(f => f.Element("required_features").All("warranty", "manual"))
```

### Empty checks

- `Empty()` → `[empty]`
- `Nempty()` → `[nempty]`

```csharp
.Filter(f => f.Element("seo_description").Empty())
.Filter(f => f.Element("summary").Nempty())
```

## Combining filters with other query parameters

```csharp
var result = await client.GetItems()
    .Filter(f => f
        .System("type").Eq("article")
        .Element("tags").Any("beginner", "intermediate"))
    .WithLanguage("en-US")
    .WithElements("title", "summary")
    .Depth(2)
    .OrderBy("system.last_modified", ascending: false)
    .Limit(10)
    .ExecuteAsync();
```

## Conditional composition (recommended)

Instead of building “filter objects”, use normal C# branching:

```csharp
var query = client.GetItems();

if (onlyArticles)
{
    query = query.Filter(f => f.System("type").Eq("article"));
}

if (!string.IsNullOrWhiteSpace(searchText))
{
    // Note: Delivery API doesn't support substring contains on strings.
    // Use [contains]/[any]/[all] only for arrays (taxonomy, linked items, multiple choice).
    query = query.Filter(f => f.Element("category").Contains(searchText));
}

var result = await query.ExecuteAsync();
```

## Troubleshooting

- **Unexpected no-results**: remember filters are ANDed; temporarily comment out filters to isolate the restrictive one.
- **Special characters**: string values are URL-encoded automatically (e.g. `&` becomes `%26`).
- **Date filtering**: the Delivery API compares date-time strings; prefer ISO-like inputs and be explicit about bounds (see Delivery docs: [Filtering parameters](https://kontent.ai/learn/docs/apis/delivery-api/filtering-parameters)).


