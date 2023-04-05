namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Represents a generic content element with value.
    /// </summary>
    public interface IContentElementValue<out T> : IContentElementValue
    {
        /// <summary>
        /// Gets the value of the content element.
        /// </summary>
        T Value { get; }
    }

    /// <summary>
    /// Represents a content element.
    /// </summary>
    public interface IContentElementValue
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