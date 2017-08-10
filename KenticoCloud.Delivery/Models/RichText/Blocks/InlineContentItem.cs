using System.Collections;
using System.Collections.Generic;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents inline content item. IEnumerable is implemented so that Html.DisplayFor is automatically bridged to the underlying ContentItem property.
    /// </summary>
    class InlineContentItem : IInlineContentItem, IEnumerable<object>
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
