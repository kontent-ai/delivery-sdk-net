using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentTypes.Element;

/// <inheritdoc cref="IMultipleChoiceElement"/>
internal sealed record MultipleChoiceElement : ContentElement, IMultipleChoiceElement
{
    /// <inheritdoc/>
    [JsonPropertyName("options")]
    public required IReadOnlyList<MultipleChoiceOption> Options { get; init; }

    IReadOnlyList<IMultipleChoiceOption> IMultipleChoiceElement.Options => Options;
}
