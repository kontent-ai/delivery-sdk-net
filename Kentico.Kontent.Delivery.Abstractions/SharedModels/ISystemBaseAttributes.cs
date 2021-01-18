namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents system base attributes of any object in Kentico Kontent.
    /// </summary>
    public interface ISystemBaseAttributes
    {
        /// <summary>
        /// Gets the codename of the object.
        /// </summary>
        string Codename { get; }

        /// <summary>
        /// Gets the identifier of the object.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the name of the object.
        /// </summary>
        string Name { get; }
    }
}
