using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems;

/// <inheritdoc/>
[DebuggerDisplay("Id = {" + nameof(Id) + "}")]
internal sealed record ContentItemSystemAttributes : IContentItemSystemAttributes
{
    /// <inheritdoc/>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("name")]
    public string Name { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("codename")]
    public required string Codename { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("sitemap_locations")]
    public IList<string>? SitemapLocation { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("last_modified")]
    public DateTime LastModified { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("language")]
    public string Language { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("collection")]
    public string Collection { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("workflow")]
    public string Workflow { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("workflow_step")]
    public string WorkflowStep { get; init; }
}

// TODO: fix accessibility modifiers