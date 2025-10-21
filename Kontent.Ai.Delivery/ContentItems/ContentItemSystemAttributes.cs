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
    public required string Name { get; init; }

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
    public required DateTime LastModified { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("language")]
    public required string Language { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("collection")]
    public required string Collection { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("workflow")]
    public required string Workflow { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("workflow_step")]
    public required string WorkflowStep { get; init; }
}
