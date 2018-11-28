using KenticoCloud.Delivery.InlineContentItems;
using System;
using System.Collections.Generic;

namespace KenticoCloud.Delivery.Extensions
{
    internal static class InlineContentItemResolverExtensions
    {
        /// <summary>
        /// Function used for registering content type specific resolvers used during processing.
        /// </summary>
        /// <param name="resolvers">Collection of resolvers.</param>
        /// <param name="resolver">Method which is used for specific content type as resolver.</param>
        /// <typeparam name="T">Content type which is resolver resolving.</typeparam>
        public static void RegisterTypeResolver<T>(this Dictionary<Type, ResolveInlineContent> resolvers, IInlineContentItemsResolver<T> resolver)
        {
            resolvers.Add(typeof(T), x => resolver.Resolve(new ResolvedContentItemData<T> { Item = (T)x }));
        }
    }
}
