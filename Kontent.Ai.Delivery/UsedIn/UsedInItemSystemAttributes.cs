using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.UsedIn;

/// <inheritdoc/>
/// <summary>
/// Initializes a new instance of the <see cref="UsedInItemSystemAttributes"/> class.
/// </summary>
[DebuggerDisplay("Id = {" + nameof(Id) + "}")]
[method: JsonConstructor]
internal sealed class UsedInItemSystemAttributes() : IUsedInItemSystemAttributes
{
    /// <inheritdoc/>
    [JsonPropertyName("id")]
    public string? Id { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("name")]
    public string? Name { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("codename")]
    public string? Codename { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("type")]
    public string? Type { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("last_modified")]
    public DateTime LastModified { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("language")]
    public string? Language { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("collection")]
    public string? Collection { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("workflow")]
    public string? Workflow { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("workflow_step")]
    public string? WorkflowStep { get; internal set; }
}