using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.UsedIn;

/// <inheritdoc cref="IUsedInItem"/>
internal sealed record UsedInItem : IUsedInItem
{
    /// <inheritdoc/>
    [JsonPropertyName("system")]
    public required UsedInItemSystemAttributes System { get; init; }

    IUsedInItemSystemAttributes IUsedInItem.System => System;
}
