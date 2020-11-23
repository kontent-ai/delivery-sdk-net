using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents system attributes of a content item.
    /// </summary>
    public interface IContentItemSystemAttributes : ISystemAttributes
    {
        /// <summary>
        /// Gets the language of the content item.
        /// </summary>
        string Language { get; }

        /// <summary>
        /// Gets a list of codenames of sitemap items to which the content item is assigned.
        /// </summary>
        IList<string> SitemapLocation { get; }

        /// <summary>
        /// Gets the codename of the content type, for example "article".
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Gets the codename of the content collection to which the content item belongs
        /// </summary>
        public string Collection { get; }
    }
}