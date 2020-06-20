using System.Collections;
using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions;

namespace Kentico.Kontent.Delivery.ContentItems.RichText
{
    internal class RichTextContent : IRichTextContent
    {
        public IEnumerable<IRichTextBlock> Blocks
        {
            get;
            private set;
        }

        public RichTextContent(IEnumerable<IRichTextBlock> blocks)
        {
            Blocks = blocks;
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
