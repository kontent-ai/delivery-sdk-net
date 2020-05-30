using System.Collections;
using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions.Models.RichText;
using Kentico.Kontent.Delivery.Abstractions.Models.RichText.Blocks;

namespace Kentico.Kontent.Delivery.StrongTyping.RichText
{
    internal class RichTextContent : IRichTextContent
    {
        public IEnumerable<IRichTextBlock> Blocks
        {
            get;
            set;
        }

        public IEnumerator<IRichTextBlock> GetEnumerator()
        {
            return Blocks.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Blocks.GetEnumerator();
        }
    }
}
