using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.Languages;

/// <inheritdoc/>
[DebuggerDisplay("Id = {" + nameof(Id) + "}")]
internal sealed record LanguageSystemAttributes : ILanguageSystemAttributes
{
    /// <inheritdoc/>
    [JsonPropertyName("codename")]
    public required string Codename { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("name")]
    public required string Name { get; init; }
}
