using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.TaxonomyGroups;

/// <inheritdoc cref="ITaxonomyGroup"/>
[DebuggerDisplay("Name = {" + nameof(System) + "." + nameof(IContentTypeSystemAttributes.Name) + "}")]
internal sealed record TaxonomyGroup : ITaxonomyGroup
{
    /// <inheritdoc/>
    [JsonPropertyName("system")]
    public required TaxonomyGroupSystemAttributes System { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("terms")]
    public required IReadOnlyList<TaxonomyTermDetails> Terms { get; init; }

    ITaxonomyGroupSystemAttributes ITaxonomyGroup.System => System;
    IReadOnlyList<ITaxonomyTermDetails> ITaxonomyGroup.Terms => Terms;
}
