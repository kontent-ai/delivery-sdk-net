using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions.Models.RichText.Blocks;

namespace Kentico.Kontent.Delivery.Abstractions.Models.RichText
{
    /// <summary>
    /// Represents rich text content in a form of structured data 
    /// </summary>
    public interface IRichTextContent : IEnumerable<IRichTextBlock>
    {
        /// <summary>
        /// List of rich text content blocks
        /// </summary>
        IEnumerable<IRichTextBlock> Blocks { get; set; }
    }
}
