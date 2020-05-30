using Kentico.Kontent.Delivery.Abstractions.Models.Type.Element;
using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions.Models.Type
{
    /// <summary>
    /// Represents a content type.
    /// </summary>
    public interface IContentType
    {
        /// <summary>
        /// Gets a dictionary that contains elements of the content type index by their codename.
        /// </summary>
        IReadOnlyDictionary<string, IContentElement> Elements { get; }

        /// <summary>
        /// Gets the system attributes of the content type.
        /// </summary>
        IContentTypeSystemAttributes System { get; }
    }
}