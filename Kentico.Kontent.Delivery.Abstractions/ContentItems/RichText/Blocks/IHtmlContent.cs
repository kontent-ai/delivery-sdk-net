﻿namespace Kentico.Kontent.Delivery.Abstractions.ContentItems.RichText.Blocks
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
