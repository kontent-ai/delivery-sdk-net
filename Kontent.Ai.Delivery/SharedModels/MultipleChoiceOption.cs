using System.Text.Json.Serialization;
using System.Diagnostics;

namespace Kontent.Ai.Delivery.SharedModels;

/// <inheritdoc/>
/// <summary>
/// Initializes a new instance of the <see cref="MultipleChoiceOption"/> class with the specified JSON data.
/// </summary>
[DebuggerDisplay("Name = {" + nameof(Name) + "}")]
[method: JsonConstructor]
internal sealed class MultipleChoiceOption() : IMultipleChoiceOption
{
    /// <inheritdoc/>
    [JsonPropertyName("name")]
    public string Name { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("codename")]
    public string Codename { get; internal set; }
}
