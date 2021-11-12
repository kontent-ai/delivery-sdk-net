using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions.ContentTypes.Element;

namespace Kentico.Kontent.Delivery.Abstractions.ContentTypes
{
    /// <summary>
    /// Represents a content type.
    /// </summary>
    public interface IContentType
    {
        /// <summary>
        /// Gets a dictionary that contains elements of the content type index by their codename.
        /// </summary>
        IDictionary<string, IContentElement> Elements { get; }

        /// <summary>
        /// Gets the system attributes of the content type.
        /// </summary>
        IContentTypeSystemAttributes System { get; }
    }
}