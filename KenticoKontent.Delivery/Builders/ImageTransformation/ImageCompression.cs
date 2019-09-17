namespace KenticoKontent.Delivery.ImageTransformation
{
    /// <summary>
    /// Types of image compression.
    /// </summary>
    public enum ImageCompression
    {
        /// <summary>
        /// Allows the original data to be perfectly reconstructed from the compressed data.
        /// </summary>
        Lossless,

        /// <summary>
        /// Irreversible compression where partial data are discarded.
        /// </summary>
        Lossy
    }
}