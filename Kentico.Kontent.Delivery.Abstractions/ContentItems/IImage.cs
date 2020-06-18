namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// A class that represents a generic image asset.
    /// </summary>
    public interface IImage
    {
        /// <summary>
        /// Gets the description of the asset.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the URL of the image.
        /// </summary>
        string Url { get; }

        /// <summary>
        /// Gets the width of the image.
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Gets the height of the image.
        /// </summary>
        int Width { get; }
    }
}