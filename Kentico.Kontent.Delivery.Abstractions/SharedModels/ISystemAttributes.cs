using System;
using System.Collections.Generic;
using System.Text;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents system attributes of any object in Kentico Kontent.
    /// </summary>
    public interface ISystemAttributes
    {
        /// <summary>
        /// Gets the codename of the object.
        /// </summary>
        string Codename { get; }

        /// <summary>
        /// Gets the identifier of the object.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the time the object was last modified.
        /// </summary>
        DateTime LastModified { get; }

        /// <summary>
        /// Gets the name of the object.
        /// </summary>
        string Name { get; }
    }
}
