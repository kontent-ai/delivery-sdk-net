using System;

namespace Kentico.Kontent.Delivery.Abstractions.Models.Type
{
    /// <summary>
    /// Represents system attributes of a content type.
    /// </summary>
    public interface IContentTypeSystemAttributes
    {
        /// <summary>
        /// Gets the codename of the content type.
        /// </summary>
        string Codename { get; }

        /// <summary>
        /// Gets the identifier of the content type.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the time the content type was last modified.
        /// </summary>
        DateTime LastModified { get; }

        /// <summary>
        /// Gets the name of the content type.
        /// </summary>
        string Name { get; }
    }
}