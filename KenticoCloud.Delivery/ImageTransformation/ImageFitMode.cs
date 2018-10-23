namespace KenticoCloud.Delivery.ImageTransformation
{
    /// <summary>
    /// Specifies the possible modes for fit transformation.
    /// </summary>
    public enum ImageFitMode
    {
        /// <summary>
        /// Resizes the image to fit within the width and height boundaries without cropping or distorting the image.
        /// </summary>
        Clip,

        /// <summary>
        /// Scales the image to fit the constraining dimensions exactly.
        /// </summary>
        Scale,

        /// <summary>
        /// Resizes the image to fill the width and height dimensions and crops any excess image data.
        /// </summary>
        Crop,
    }
}