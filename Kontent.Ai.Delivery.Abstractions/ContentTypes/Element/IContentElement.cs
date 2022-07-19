namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents a content element.
    /// </summary>
    public interface IContentElement
    {
        /// <summary>
        /// Gets the codename of the content element.
        /// </summary>
        string Codename { get; }

        /// <summary>
        /// Gets the name of the content element.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the type of the content element, for example "multiple_choice".
        /// </summary>
        string Type { get; }
    }
}