using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.UsedIn;

/// <inheritdoc cref="IUsedInItem"/>
internal sealed class UsedInItem : IUsedInItem
{
    /// <inheritdoc/>
    [JsonPropertyName("system")]
    public required UsedInItemSystemAttributes System { get; set; }

    IUsedInItemSystemAttributes IUsedInItem.System => System;
}