using System.Collections;
using System.Collections.Generic;

namespace Kentico.Kontent.Delivery
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
