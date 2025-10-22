using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.TaxonomyGroups;

/// <inheritdoc/>
/// <summary>
/// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
/// </summary>
[DebuggerDisplay("Name = {" + nameof(System) + "." + nameof(IContentTypeSystemAttributes.Name) + "}")]
internal sealed record TaxonomyGroup : ITaxonomyGroup
{
    /// <inheritdoc/>
    [JsonPropertyName("system")]
    public TaxonomyGroupSystemAttributes System { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("terms")]
    public IList<TaxonomyTermDetails> Terms { get; init; }

    ITaxonomyGroupSystemAttributes ITaxonomyGroup.System => System;

    IList<ITaxonomyTermDetails> ITaxonomyGroup.Terms => [.. Terms.Cast<ITaxonomyTermDetails>()];
}
