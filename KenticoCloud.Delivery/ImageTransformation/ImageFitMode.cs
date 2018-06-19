using System.Collections.Generic;

namespace KenticoCloud.Delivery.ImageTransformation
{
    /// <summary>
    /// Specifies the possible modes for fit transformation.
    /// </summary>
    public struct ImageFitMode
    {
        private static readonly Dictionary<string, ImageFitMode> TranslateDictionary = new Dictionary<string, ImageFitMode>();
        private readonly string _value;

        /// <summary>
        /// Resizes the image to fit within the width and height boundaries without cropping or distorting the image.
        /// </summary>
        public static readonly ImageFitMode Clip = new ImageFitMode("clip");

        /// <summary>
        /// Scales the image to fit the constraining dimensions exactly.
        /// </summary>
        public static readonly ImageFitMode Scale = new ImageFitMode("scale");

        /// <summary>
        /// Resizes the image to fill the width and height dimensions and crops any excess image data.
        /// </summary>
        public static readonly ImageFitMode Crop = new ImageFitMode("crop");

        private ImageFitMode(string value)
        {
            _value = value;
            TranslateDictionary[value] = this;
        }

        /// <summary>
        /// Gets the value associated with this value.
        /// </summary>
        /// <param name="value">Value to parse.</param>
        /// <param name="fitMode"><see cref="ImageFitMode"/> representation of the <paramref name="value"/>.</param>
        /// <returns>Whether the value was parsed successfully.</returns>
        public static bool TryParse(string value, out ImageFitMode fitMode) => TranslateDictionary.TryGetValue(value, out fitMode);

        /// <inheritdoc />
        public override string ToString() => _value;
    }
}