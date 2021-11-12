using System;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.ContentItems.InlineContentItems;

namespace Kentico.Kontent.Delivery.Tests.Factories
{
    internal class InlineContentItemsResolverFactory
    {
        private static readonly Lazy<InlineContentItemsResolverFactory> LazyInstance = new Lazy<InlineContentItemsResolverFactory>(() => new InlineContentItemsResolverFactory());

        public static InlineContentItemsResolverFactory Instance => LazyInstance.Value;

        private InlineContentItemsResolverFactory()
        {            
        }

        public IInlineContentItemsResolver<TContentItem> ResolveToMessage<TContentItem>(string message)
            => ResolveTo<TContentItem>(_ => message);

        public IInlineContentItemsResolver<object> ResolveByDefaultToMessage(string message)
            => ResolveToMessage<object>(message);

        public IInlineContentItemsResolver<TContentItem> ResolveToType<TContentItem>(bool acceptNull = false)
            => ResolveTo<TContentItem>(item => acceptNull && item == null 
                ? typeof(TContentItem).FullName 
                : item.GetType().FullName);

        public IInlineContentItemsResolver<object> ResolveByDefaultToType()
            => ResolveToType<object>();

        public IInlineContentItemsResolver<TContentItem> ResolveTo<TContentItem>(Func<TContentItem, string> resultSelector)
            => new SimpleResolver<TContentItem>(resultSelector);

        public IInlineContentItemsResolver<object> ResolveByDefaultTo(Func<object, string> resultSelector)
            => ResolveTo(resultSelector);

        private class SimpleResolver<TContentItem> : IInlineContentItemsResolver<TContentItem>
        {
            private readonly Func<TContentItem, string> _resultSelector;

            public SimpleResolver(Func<TContentItem, string> resultSelector)
                => _resultSelector = resultSelector;

            public string Resolve(TContentItem item)
                => _resultSelector(item);
        }
    }
}
