using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.TaxonomyGroups;

/// <inheritdoc/>
/// <summary>
/// Initializes a new instance of the <see cref="TaxonomyGroupSystemAttributes"/> class.
/// </summary>
[DebuggerDisplay("Id = {" + nameof(Id) + "}")]
[method: JsonConstructor]
internal sealed class TaxonomyGroupSystemAttributes() : ITaxonomyGroupSystemAttributes
{
    /// <inheritdoc/>
    [JsonPropertyName("id")]
    public string Id { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("name")]
    public string Name { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("codename")]
    public string Codename { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("last_modified")]
    public DateTime LastModified { get; internal set; }
}
