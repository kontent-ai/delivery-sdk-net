namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// Represents HTML content rich text block
    /// </summary>
    public interface IHtmlContent : IRichTextBlock
    {
        /// <summary>
        /// HTML code
        /// </summary>
        string Html { get; }
    }
}
