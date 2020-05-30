namespace Kentico.Kontent.Delivery.Abstractions.ContentItems
{
    /// <summary>
    /// A class that represents a generic image asset.
    /// </summary>
    public interface IImage
    {
        /// <summary>
        /// Gets the description of the asset.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Gets the URL of the image.
        /// </summary>
        string Url { get; set; }

        /// <summary>
        /// Gets the width of the image.
        /// </summary>
        int Height { get; set; }

        /// <summary>
        /// Gets the height of the image.
        /// </summary>
        int Width { get; set; }
    }
}