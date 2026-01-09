namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents system attributes of a content item.
/// </summary>
public interface IContentItemSystemAttributes : ISystemAttributes
{
    /// <summary>
    /// Gets the language of the content item.
    /// </summary>
    string Language { get; }

    /// <summary>
    /// Gets a list of codenames of sitemap items to which the content item is assigned.
    /// </summary>
    [Obsolete("Sitemap locations are deprecated and will be removed in the future.")]
    IList<string>? SitemapLocation { get; }

    /// <summary>
    /// Gets the codename of the content type, for example "article".
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Gets the codename of the content collection to which the content item belongs.
    /// </summary>
    public string Collection { get; }

    /// <summary>
    /// Gets the codename of the workflow which the content item is assigned to.
    /// May be null for components in linked items.
    /// </summary>
    public string? Workflow { get; }

    /// <summary>
    /// Gets the codename of the workflow step which the content item is assigned to.
    /// May be null for components in linked items.
    /// </summary>
    public string? WorkflowStep { get; }
}