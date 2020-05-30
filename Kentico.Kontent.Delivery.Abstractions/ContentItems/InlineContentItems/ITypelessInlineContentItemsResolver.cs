using System;
using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Kontent.Delivery.Abstractions.ContentItems.InlineContentItems
{
    /// <summary>
    /// An interface, implemented to be registered in an collection passed to <see cref="IInlineContentItemsProcessor"/>.
    /// Such collection provide the processor with generic resolvers for otherwise specific content type of inline content item.
    /// </summary>
    /// <seealso cref="IInlineContentItemsResolver{T}"/>
    /// <remarks>
    /// This interface allows containers implementing the <see cref="IServiceCollection"/> interface to
    /// register dependencies required for <see cref="IInlineContentItemsProcessor"/> instantiation.
    /// </remarks>
    public interface ITypelessInlineContentItemsResolver
    {
        /// <summary>
        /// Type of an inline content item.
        /// </summary>
        Type ContentItemType { get; }

        /// <summary>
        /// Transformation function resolving an inline content item of a given inline content item.
        /// </summary>
        /// <param name="item">An inline content item of given <see cref="ContentItemType"/></param>
        /// <returns>String representation of the <paramref name="item"/></returns>
        string ResolveItem(object item);
    }
}
