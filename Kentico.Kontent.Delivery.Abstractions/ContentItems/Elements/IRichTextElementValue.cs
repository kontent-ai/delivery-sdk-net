using System;
using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// An element representing a rich-text value. In addition to formatted text, 
    /// the rich text element's value property can contain objects representing images, components, content items, and links to content items.
    /// </summary>
    public interface IRichTextElementValue : IContentElementValue<string>
    {
        /// <summary>
        /// The images inserted into the rich text element. Each object in the collection contains the inserted image's metadata.
        /// </summary>
        IDictionary<Guid, IInlineImage> Images { get; }

        /// <summary>
        /// The hyperlinks in the text that point to content items. Each object in the collection contains the linked item's metadata.
        /// </summary>
        IDictionary<Guid, IContentLink> Links { get; }

        /// <summary>
        /// A collection of components and content items inserted into the text. Each string in the array is a codename of content item or component.
        /// </summary>
        List<string> ModularContent { get; }
    }
}
