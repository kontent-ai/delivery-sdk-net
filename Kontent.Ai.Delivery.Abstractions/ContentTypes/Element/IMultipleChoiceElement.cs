namespace Kontent.Ai.Delivery.Abstractions;

internal interface IMultipleChoiceElement : IContentElement
{
    /// <summary>
    /// Gets a list of predefined options for the Multiple choice content element; otherwise, an empty list.
    /// </summary>
    IReadOnlyList<IMultipleChoiceOption> Options { get; }
}
