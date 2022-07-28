using System;

namespace Kontent.Ai.Delivery.Abstractions
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
