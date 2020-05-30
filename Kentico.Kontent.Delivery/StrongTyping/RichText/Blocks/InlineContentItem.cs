using System.Collections;
using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions.Models.RichText.Blocks;

namespace Kentico.Kontent.Delivery.StrongTyping.RichText.Blocks
{
    /// <summary>
    /// Represents inline content item. IEnumerable is implemented so that Html.DisplayFor is automatically bridged to the underlying ContentItem property.
    /// </summary>
    internal class InlineContentItem : IInlineContentItem, IEnumerable<object>
    {
        public object ContentItem
        {
            get;
            set;
        }

        public IEnumerator<object> GetEnumerator()
        {
            yield return ContentItem;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return ContentItem;
        }
    }
}
