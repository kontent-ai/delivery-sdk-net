using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentTypes.Element;

/// <summary>
/// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
/// </summary>
internal sealed record MultipleChoiceElement() : ContentElement, IMultipleChoiceElement
{
    /// <inheritdoc/>
    [JsonPropertyName("options")]
    public required IReadOnlyList<MultipleChoiceOption> Options { get; init; }

    IReadOnlyList<IMultipleChoiceOption> IMultipleChoiceElement.Options => Options;
}