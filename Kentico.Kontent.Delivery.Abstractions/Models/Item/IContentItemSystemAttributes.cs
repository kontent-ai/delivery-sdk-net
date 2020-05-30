using System;
using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions.Models.Item
{
    /// <summary>
    /// Represents system attributes of a content item.
    /// </summary>
    public interface IContentItemSystemAttributes
    {
        /// <summary>
        /// Gets the codename of the content item.
        /// </summary>
        string Codename { get; }

        /// <summary>
        /// Gets the identifier of the content item.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the language of the content item.
        /// </summary>
        string Language { get; set; }

        /// <summary>
        /// Gets the time the content item was last modified.
        /// </summary>
        DateTime LastModified { get; set; }

        /// <summary>
        /// Gets the name of the content item.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a list of codenames of sitemap items to which the content item is assigned.
        /// </summary>
        IReadOnlyList<string> SitemapLocation { get; }

        /// <summary>
        /// Gets the codename of the content type, for example "article".
        /// </summary>
        string Type { get; }
    }
}