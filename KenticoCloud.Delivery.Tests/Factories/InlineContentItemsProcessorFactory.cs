using System.Collections.Generic;
using KenticoCloud.Delivery.InlineContentItems;

namespace KenticoCloud.Delivery.Tests.Factories
{
    internal class InlineContentItemsProcessorFactory
    {
        private readonly IList<ITypelessInlineContentItemsResolver> _resolvers = new List<ITypelessInlineContentItemsResolver>();

        public static InlineContentItemsProcessor Create()
            => new InlineContentItemsProcessorFactory().Build();

        public static InlineContentItemsProcessorFactory WithResolver<TContentItem>(IInlineContentItemsResolver<TContentItem> resolver)
            => new InlineContentItemsProcessorFactory().AndResolver(resolver);

        public static InlineContentItemsProcessorFactory WithResolver<TContentItem, TResolver>()
            where TResolver : IInlineContentItemsResolver<TContentItem>, new()
            => new InlineContentItemsProcessorFactory().AndResolver<TContentItem, TResolver>();

        private InlineContentItemsProcessorFactory()
        { }

        public InlineContentItemsProcessorFactory AndResolver<TContentItem>(IInlineContentItemsResolver<TContentItem> resolver)
        {
            var typelessResolver = TypelessInlineContentItemsResolver.Create(resolver);
            _resolvers.Add(typelessResolver);

            return this;
        }

        public InlineContentItemsProcessorFactory AndResolver<TContentItem, TResolver>()
            where TResolver : IInlineContentItemsResolver<TContentItem>, new()
            => AndResolver(new TResolver());

        public InlineContentItemsProcessor Build()
            => new InlineContentItemsProcessor(_resolvers);
    }
}
