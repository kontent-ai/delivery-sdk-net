using System;

namespace Kentico.Kontent.Delivery.Abstractions.ContentItems.RichText.Blocks
{
    /// <summary>
    /// Represents inline image block within rich text
    /// </summary>
    public interface IInlineImage : IRichTextBlock, IImage
    {
        /// <summary>
        /// Unique image identifier.
        /// </summary>
        public Guid ImageId { get; }
    }
}
