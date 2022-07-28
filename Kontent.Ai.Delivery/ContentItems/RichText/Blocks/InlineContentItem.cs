using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Blocks
{
    /// <summary>
    /// Represents inline content item. IEnumerable is implemented so that Html.DisplayFor is automatically bridged to the underlying ContentItem property.
    /// </summary>
    internal class InlineContentItem : List<object>, IInlineContentItem
    {
        public object ContentItem
        {
            get
            {
                return this[0];
            }
        }

        [JsonConstructor]
        private InlineContentItem()
        {
        }

        public InlineContentItem(object contentItem)
        {
            Add(contentItem);
        }
    }
}
