using System;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.ContentItems.InlineContentItems;
using Kentico.Kontent.Delivery.Builders.DeliveryClient;
using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Kontent.Delivery.ContentItems.InlineContentItems
{
    /// <summary>
    /// Strips an <see cref="IInlineContentItemsResolver{T}"/> of its generic type, so it can be used generically by <see cref="InlineContentItemsProcessor"/>.
    /// </summary>
    /// <remarks>
    /// This class can be used with other container than <see cref="IServiceCollection"/> to register dependencies required for
    /// <see cref="InlineContentItemsProcessor"/> instantiation. The <see cref="Extensions.ServiceCollectionExtensions.AddDeliveryInlineContentItemsResolver{TContentItem}"/>
    /// or <see cref="IOptionalClientSetup.WithInlineContentItemsResolver{T}"/> should always be used with <see cref="IServiceCollection"/>
    /// or <see cref="DeliveryClientBuilder"/> respectively.
    /// </remarks>
    internal class TypelessInlineContentItemsResolver : ITypelessInlineContentItemsResolver
    {
        /// <summary>
        /// Creates new instance of <see cref="TypelessInlineContentItemsResolver"/> for given <paramref name="resolver"/>.
        /// </summary>
        /// <typeparam name="TContentItem">Content item type the <paramref name="resolver"/> works with</typeparam>
        /// <param name="resolver">Resolver for specific content type of inline content item</param>
        public static ITypelessInlineContentItemsResolver Create<TContentItem>(IInlineContentItemsResolver<TContentItem> resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            string Resolve(object item) => resolver.Resolve((TContentItem)item);

            return new TypelessInlineContentItemsResolver(Resolve, typeof(TContentItem));
        }

        private readonly Func<object, string> _resolveItem;

        /// <inheritdoc cref="ITypelessInlineContentItemsResolver.ContentItemType"/>
        public Type ContentItemType { get; }


        /// <inheritdoc cref="ITypelessInlineContentItemsResolver.ResolveItem"/>
        public string ResolveItem(object item)
            => _resolveItem(item);

        private TypelessInlineContentItemsResolver(Func<object, string> resolveItem, Type contentItemType)
        {
            _resolveItem = resolveItem;
            ContentItemType = contentItemType;
        }
    }
}