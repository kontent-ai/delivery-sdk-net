using System;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents asset rendition data.
    /// </summary>
    public class IAssetRendition
    {
        /// <summary>
        /// Gets id of rendition.
        /// </summary>
        string RenditionId { get; }

        /// <summary>
        /// Gets id of asset that uses this rendition.
        /// </summary>
        string AssetId { get; }

        /// <summary>
        /// Gets rendition preset id.
        /// </summary>
        string PresetId { get; }

        /// <summary>
        /// Gets the asset size in bytes.
        /// </summary>
        long Size { get; }

        /// <summary>
        /// Gets image asset width in pixels.
        /// </summary>
        int ImageWidth { get; }

        /// <summary>
        /// Gets image asset height in pixels.
        /// </summary>
        int ImageHeight { get; }
        
        /// <summary>
        /// Gets query string parameters needed for image transformations.
        /// </summary>
        string TransformationQueryString { get;}

        /// <summary>
        /// Gets timestamp of rendition creation.
        /// </summary>
        DateTime Created { get; }

        /// <summary>
        /// Gets timestamp of last rendition modification.
        /// </summary>
        DateTime LastModified { get; }
    }
}