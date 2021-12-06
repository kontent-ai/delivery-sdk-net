namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents asset rendition data.
    /// </summary>
    public interface IAssetRendition
    {
        /// <summary>
        /// Gets id of rendition.
        /// </summary>
        string RenditionId { get; }

        /// <summary>
        /// Gets rendition preset id.
        /// </summary>
        string PresetId { get; }

        /// <summary>
        /// Gets rendition width in pixels.
        /// </summary>
        int Width { get; }

        /// <summary>
        /// Gets rendition height in pixels.
        /// </summary>
        int Height { get; }
        
        /// <summary>
        /// Gets query string parameters used for image transformations.
        /// </summary>
        string Query { get;}
    }
}