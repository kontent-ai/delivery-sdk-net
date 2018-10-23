using System;

namespace KenticoCloud.Delivery.ImageTransformation
{
    /// <summary>
    /// Supported image formats.
    /// </summary>
    public enum ImageFormat
    {
        /// <summary>
        /// Graphics Interchange Format.
        /// </summary>
        Gif,

        /// <summary>
        /// Portable Network Graphics.
        /// </summary>
        Png,

        /// <summary>
        /// Portable Network Graphics palette variant with 8-bit transparency and 256 colors.
        /// </summary>
        Png8,

        /// <summary>
        /// JPEG.
        /// </summary>
        Jpg,

        /// <summary>
        /// Progressive JPEG.
        /// </summary>
        Pjpg,

        /// <summary>
        /// WebP.
        /// </summary>
        Webp,
    }
}