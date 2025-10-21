using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentTypes.Element;

/// <summary>
/// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
/// </summary>
[method: JsonConstructor]
internal sealed record MultipleChoiceElement() : ContentElement, IMultipleChoiceElement
{
    /// <inheritdoc/>
    [JsonPropertyName("options")]
    public IList<IMultipleChoiceOption> Options { get; init; }
}