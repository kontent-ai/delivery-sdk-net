namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;

/// <summary>
/// System property paths available for content items.
/// </summary>
public static class ItemSystemProperties
{
    /// <summary>
    /// Content item ID (GUID format) - system.id.
    /// </summary>
    public const string Id = "system.id";

    /// <summary>
    /// Content item codename - system.codename.
    /// </summary>
    public const string Codename = "system.codename";

    /// <summary>
    /// Content type codename - system.type.
    /// </summary>
    public const string Type = "system.type";

    /// <summary>
    /// Content item name - system.name.
    /// </summary>
    public const string Name = "system.name";

    /// <summary>
    /// Last modification timestamp - system.last_modified.
    /// </summary>
    public const string LastModified = "system.last_modified";

    /// <summary>
    /// Language variant codename - system.language.
    /// </summary>
    public const string Language = "system.language";

    /// <summary>
    /// Collection codename - system.collection.
    /// </summary>
    public const string Collection = "system.collection";

    /// <summary>
    /// Workflow codename - system.workflow.
    /// </summary>
    public const string Workflow = "system.workflow";

    /// <summary>
    /// Workflow step codename - system.workflow_step.
    /// </summary>
    public const string WorkflowStep = "system.workflow_step";

    /// <summary>
    /// Sitemap locations array - system.sitemap_locations.
    /// </summary>
    public const string SitemapLocations = "system.sitemap_locations";
}

/// <summary>
/// System property paths available for content types (limited set).
/// </summary>
public static class TypeSystemProperties
{
    /// <summary>
    /// Content type codename - system.codename.
    /// </summary>
    public const string Codename = "system.codename";

    /// <summary>
    /// Last modification timestamp - system.last_modified.
    /// </summary>
    public const string LastModified = "system.last_modified";
}

/// <summary>
/// System property paths available for taxonomy groups (very limited set).
/// </summary>
public static class TaxonomySystemProperties
{
    /// <summary>
    /// Taxonomy group codename - system.codename.
    /// </summary>
    public const string Codename = "system.codename";

    /// <summary>
    /// Last modification timestamp - system.last_modified.
    /// </summary>
    public const string LastModified = "system.last_modified";
}

/// <summary>
/// Helper methods for constructing element property paths.
/// </summary>
public static class ElementProperties
{
    /// <summary>
    /// Creates an element property path for the specified element codename.
    /// </summary>
    /// <param name="codename">The element codename.</param>
    /// <returns>The element property path (elements.{codename}).</returns>
    public static string For(string codename) => $"elements.{codename}";
}