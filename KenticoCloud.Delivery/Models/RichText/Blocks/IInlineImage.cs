namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents inline image block within rich text
    /// </summary>
    public interface IInlineImage : IRichTextBlock
    {
        /// <summary>
        /// Alternate text
        /// </summary>
        string AltText { get; set; }

        /// <summary>
        /// Source URL of the image
        /// </summary>
        string Src { get; set; }
    }
}
