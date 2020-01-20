using System;
using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions.InlineContentItems;
using Kentico.Kontent.Delivery.InlineContentItems;

namespace Kentico.Kontent.Delivery.Tests.Factories
{
    internal class InlineContentItemsProcessorFactory
    {
        private readonly IList<ITypelessInlineContentItemsResolver> _resolvers = new List<ITypelessInlineContentItemsResolver>();

        public static InlineContentItemsProcessor Create()
            => new InlineContentItemsProcessorFactory().Build();

        public static InlineContentItemsProcessorFactory WithSimpleResolver<TContentItem>(Func<TContentItem, string> valueSelector)
            => WithResolver(factory => factory.ResolveTo(valueSelector));

        public static InlineContentItemsProcessorFactory WithResolver<TContentItem>(Func<InlineContentItemsResolverFactory, IInlineContentItemsResolver<TContentItem>> resolverSelector)
            => new InlineContentItemsProcessorFactory().AndResolver(resolverSelector);

        private InlineContentItemsProcessorFactory()
        { }

        public InlineContentItemsProcessorFactory AndResolver<TContentItem>(Func<InlineContentItemsResolverFactory, IInlineContentItemsResolver<TContentItem>> resolverSelector)
            => AndResolver(resolverSelector(InlineContentItemsResolverFactory.Instance));


        public InlineContentItemsProcessor Build()
            => new InlineContentItemsProcessor(_resolvers);

        private InlineContentItemsProcessorFactory AndResolver<TContentItem>(IInlineContentItemsResolver<TContentItem> resolver)
        {
            var typelessResolver = TypelessInlineContentItemsResolver.Create(resolver);
            _resolvers.Add(typelessResolver);

            return this;
        }
    }
}
