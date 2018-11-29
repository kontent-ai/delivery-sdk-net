using System;
using KenticoCloud.Delivery.Builders.DeliveryClient;

namespace KenticoCloud.Delivery.InlineContentItems
{
    /// <summary>
    /// An interface, implemented to be registered in an collection passed to <see cref="InlineContentItemsProcessor"/>.
    /// Such collection provide the processor with generic resolvers for otherwise specific content type of inline content item.
    /// </summary>
    /// <seealso cref="IInlineContentItemsResolver{T}"/>
    /// <remarks>
    /// The <see cref="ServiceCollectionExtensions.AddDeliveryInlineContentItemsResolver{TContentItem}"/> or <see cref="IOptionalClientSetup.WithInlineContentItemsResolver{T}"/>
    /// (in <see cref="DeliveryClientBuilder"/>) should always be used to create new instances of this interface. All such instances will be registered in an dependency
    /// container and later used for <see cref="InlineContentItemsProcessor"/> instantiation.
    /// </remarks>
    internal interface ITypelessInlineContentItemsResolver
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
