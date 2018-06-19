using System.Collections.Generic;

namespace KenticoCloud.Delivery.ImageTransformation
{
    /// <summary>
    /// Supported image formats.
    /// </summary>
    public struct ImageFormat
    {
        private static readonly Dictionary<string, ImageFormat> TranslateDictionary = new Dictionary<string, ImageFormat>();
        private readonly string _value;

        /// <summary>
        /// Graphics Interchange Format.
        /// </summary>
        public static readonly ImageFormat Gif = new ImageFormat("gif");

        /// <summary>
        /// Portable Network Graphics.
        /// </summary>
        public static readonly ImageFormat Png = new ImageFormat("png");

        /// <summary>
        /// Portable Network Graphics palette variant with 8-bit transparency and 256 colors.
        /// </summary>
        public static readonly ImageFormat Png8 = new ImageFormat("png8");

        /// <summary>
        /// JPEG.
        /// </summary>
        public static readonly ImageFormat Jpg = new ImageFormat("jpg");

        /// <summary>
        /// Progressive JPEG.
        /// </summary>
        public static readonly ImageFormat Pjpg = new ImageFormat("pjpg");

        /// <summary>
        /// WebP.
        /// </summary>
        public static readonly ImageFormat Webp = new ImageFormat("webp");

        private ImageFormat(string value)
        {
            _value = value;
            TranslateDictionary[value] = this;
        }

        /// <summary>
        /// Gets the value associated with this value.
        /// </summary>
        /// <param name="value">Value to parse.</param>
        /// <param name="format"><see cref="ImageFormat"/> representation of the <paramref name="value"/>.</param>
        /// <returns>Whether the value was parsed successfully.</returns>
        public static bool TryParse(string value, out ImageFormat format) => TranslateDictionary.TryGetValue(value, out format);

        /// <inheritdoc />
        public override string ToString() => _value;
    }
}