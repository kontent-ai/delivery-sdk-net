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
