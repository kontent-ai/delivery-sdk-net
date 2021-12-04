using System;

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
        /// Gets image asset width in pixels.
        /// </summary>
        int Width { get; }

        /// <summary>
        /// Gets image asset height in pixels.
        /// </summary>
        int Height { get; }
        
        /// <summary>
        /// Gets query string parameters needed for image transformations.
        /// </summary>
        string Query { get;}
    }
}