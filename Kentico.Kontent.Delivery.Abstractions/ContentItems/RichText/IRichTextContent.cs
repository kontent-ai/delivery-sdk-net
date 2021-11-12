using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions.ContentItems.RichText.Blocks;

namespace Kentico.Kontent.Delivery.Abstractions.ContentItems.RichText
{
    /// <summary>
    /// Represents rich text content in a form of structured data 
    /// </summary>
    public interface IRichTextContent : IEnumerable<IRichTextBlock>
    {
    }
}
