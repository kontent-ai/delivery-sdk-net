using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents a digital asset, such as a document or image.
    /// </summary>
    public interface IAsset: IImage
    {
        /// <summary>
        /// Gets the name of the asset.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the asset size in bytes.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Gets the media type of the asset, for example "image/jpeg".
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Gets the asset renditions list.
        /// </summary>
        IEnumerable<IAssetRendition> Renditions { get; }
    }
}