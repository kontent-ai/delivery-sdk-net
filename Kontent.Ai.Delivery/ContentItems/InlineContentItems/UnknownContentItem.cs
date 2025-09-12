namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Type used to identify inline content items which don't have corresponding model.
/// </summary>
/// <remarks>
/// Creates an instance of <see cref="UnknownContentItem"/> which represents content item with no corresponding model.
/// </remarks>
/// <param name="type">The unresolvable type.</param>
public class UnknownContentItem(string type)
{
    /// <summary>
    /// Represents the content type that has no corresponding model
    /// </summary>
    public string Type { get; set; } = type;
}
