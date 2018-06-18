namespace KenticoCloud.Delivery.ImageOptimization
{
    /// <summary>
    /// Specifies the possible modes for fit tranformation.
    /// </summary>
    public struct ImageFitMode
    {
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
        }

        /// <inheritdoc />
        public override string ToString() => _value;
    }
}