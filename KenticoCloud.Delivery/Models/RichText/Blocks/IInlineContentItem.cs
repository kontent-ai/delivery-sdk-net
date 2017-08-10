namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents inline content item block within rich text
    /// </summary>
    public interface IInlineContentItem : IRichTextBlock
    {
        /// <summary>
        /// Referenced content item
        /// </summary>
        object ContentItem { get; set; }
    }
}
