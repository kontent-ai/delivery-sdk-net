namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents a content element.
    /// </summary>
    public interface IContentElementValue<T> : IContentElement
    {
        /// <summary>
        /// Gets the value of the content element.
        /// </summary>
        T Value { get; }
    }
}