using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.TaxonomyGroups;

/// <inheritdoc/>
/// <summary>
/// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
/// </summary>
[DebuggerDisplay("Name = {" + nameof(System) + "." + nameof(IContentTypeSystemAttributes.Name) + "}")]
[method: JsonConstructor]
internal sealed class TaxonomyGroup(ITaxonomyGroupSystemAttributes system, IList<ITaxonomyTermDetails> terms) : ITaxonomyGroup
{
    /// <inheritdoc/>
    [JsonPropertyName("system")]
    public ITaxonomyGroupSystemAttributes System { get; internal set; } = system;

    /// <inheritdoc/>
    [JsonPropertyName("terms")]
    public IList<ITaxonomyTermDetails> Terms { get; internal set; } = terms;
}
