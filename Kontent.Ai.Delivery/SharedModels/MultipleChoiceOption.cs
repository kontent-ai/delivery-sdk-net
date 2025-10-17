using System.Text.Json.Serialization;
using System.Diagnostics;

namespace Kontent.Ai.Delivery.SharedModels;

/// <inheritdoc/>
/// <summary>
/// Initializes a new instance of the <see cref="MultipleChoiceOption"/> class with the specified JSON data.
/// </summary>
[DebuggerDisplay("Name = {" + nameof(Name) + "}")]
[method: JsonConstructor]
public sealed record MultipleChoiceOption() : IMultipleChoiceOption
{
    /// <inheritdoc/>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("codename")]
    public required string Codename { get; init; }
}
