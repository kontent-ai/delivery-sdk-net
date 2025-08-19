namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;

/// <summary>
/// Common abstraction for any queryable property path.
/// </summary>
public interface IPropertyPath
{
    /// <summary>String form used in requests (e.g., "system.id", "elements.title").</summary>
    string Serialize();
}

/// <summary>
/// Strongly-typed system property path for content items.
/// </summary>
public readonly record struct ItemSystemPath
    : IPropertyPath
{
    private readonly string _value;
    private ItemSystemPath(string value) => _value = value;

    /// <summary>
    /// Content item ID (GUID format) - system.id.
    /// </summary>
    public static ItemSystemPath Id { get; } = new("system.id");

    /// <summary>
    /// Content item codename - system.codename.
    /// </summary>
    public static ItemSystemPath Codename { get; } = new("system.codename");

    /// <summary>
    /// Content type codename - system.type.
    /// </summary>
    public static ItemSystemPath Type { get; } = new("system.type");

    /// <summary>
    /// Content item name - system.name.
    /// </summary>
    public static ItemSystemPath Name { get; } = new("system.name");

    /// <summary>
    /// Last modification timestamp - system.last_modified.
    /// </summary>
    public static ItemSystemPath LastModified { get; } = new("system.last_modified");

    /// <summary>
    /// Language variant codename - system.language.
    /// </summary>
    public static ItemSystemPath Language { get; } = new("system.language");

    /// <summary>
    /// Collection codename - system.collection.
    /// </summary>
    public static ItemSystemPath Collection { get; } = new("system.collection");

    /// <summary>
    /// Workflow codename - system.workflow.
    /// </summary>
    public static ItemSystemPath Workflow { get; } = new("system.workflow");

    /// <summary>
    /// Workflow step codename - system.workflow_step.
    /// </summary>
    public static ItemSystemPath WorkflowStep { get; } = new("system.workflow_step");

    /// <summary>
    /// Sitemap locations array - system.sitemap_locations.
    /// </summary>
    public static ItemSystemPath SitemapLocations { get; } = new("system.sitemap_locations");

    /// <inheritdoc />
    public string Serialize() => _value;
}

/// <summary>
/// Strongly-typed system property path for content types.
/// </summary>
public readonly record struct TypeSystemPath
    : IPropertyPath
{
    private readonly string _value;
    private TypeSystemPath(string value) => _value = value;

    /// <summary>
    /// Content type ID (GUID format) - system.id.
    /// </summary>
    public static TypeSystemPath Id { get; } = new("system.id");

    /// <summary>
    /// Content type codename - system.codename.
    /// </summary>
    public static TypeSystemPath Codename { get; } = new("system.codename");

    /// <summary>
    /// Content type name - system.name.
    /// </summary>
    public static TypeSystemPath Name { get; } = new("system.name");

    /// <summary>
    /// Last modification timestamp - system.last_modified.
    /// </summary>
    public static TypeSystemPath LastModified { get; } = new("system.last_modified");

    /// <inheritdoc />
    public string Serialize() => _value;
}

/// <summary>
/// Strongly-typed system property path for taxonomy groups.
/// </summary>
public readonly record struct TaxonomySystemPath
    : IPropertyPath
{
    private readonly string _value;
    private TaxonomySystemPath(string value) => _value = value;

    /// <summary>
    /// Taxonomy group ID (GUID format) - system.id.
    /// </summary>
    public static TaxonomySystemPath Id { get; } = new("system.id");

    /// <summary>
    /// Taxonomy group codename - system.codename.
    /// </summary>
    public static TaxonomySystemPath Codename { get; } = new("system.codename");

    /// <summary>
    /// Taxonomy group name - system.name.
    /// </summary>
    public static TaxonomySystemPath Name { get; } = new("system.name");

    /// <summary>
    /// Last modification timestamp - system.last_modified.
    /// </summary>
    public static TaxonomySystemPath LastModified { get; } = new("system.last_modified");

    /// <inheritdoc />
    public string Serialize() => _value;
}

/// <summary>
/// Strongly-typed element property path.
/// </summary>
public readonly record struct ElementPath
    : IPropertyPath
{
    private readonly string _value;

    private ElementPath(string value) => _value = value;

    // internal factory: only code in this assembly can create ElementPath
    internal static ElementPath FromCodename(string codename)
        => new($"elements.{codename}");

    /// <summary>
    /// Returns the string representation of the element path.
    /// </summary>
    /// <inheritdoc />
    public string Serialize() => _value;
}

/// <summary>
/// Helper methods for constructing element property paths.
/// </summary>
public static class Elements
{
    /// <summary>
    /// Creates an element property path for the specified element codename.
    /// </summary>
    /// <param name="codename">The element codename.</param>
    /// <returns>The element property path (elements.{codename}).</returns>
    public static ElementPath GetPath(string codename) => ElementPath.FromCodename(codename);
}