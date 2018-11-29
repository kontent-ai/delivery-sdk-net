using System;

namespace KenticoCloud.Delivery.InlineContentItems
{
    /// <summary>
    /// Strips an <see cref="IInlineContentItemsResolver{T}"/> of its generic type, so it can be used generically by <see cref="InlineContentItemsProcessor"/>.
    /// </summary>
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

            string Resolve(object item) => resolver.Resolve(new ResolvedContentItemData<TContentItem> { Item = (TContentItem)item });

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