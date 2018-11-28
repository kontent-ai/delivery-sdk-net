using System;
using System.Collections.Generic;

namespace KenticoCloud.Delivery.InlineContentItems
{
    /// <summary>
    /// Collection of inline content item resolvers indexed by types that they resolve.
    /// </summary>
    internal interface IInlineContentItemsResolverCollection
    {
        /// <summary>
        /// Collection of inline content item resolvers indexed by types that they resolve.
        /// </summary>
        Dictionary<Type, ResolveInlineContent> InlineContentItemsResolvers { get; }
    }
}
