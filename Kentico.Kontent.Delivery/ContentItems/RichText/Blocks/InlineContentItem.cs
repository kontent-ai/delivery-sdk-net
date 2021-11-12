using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.ContentItems.RichText.Blocks;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.ContentItems.RichText.Blocks
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
