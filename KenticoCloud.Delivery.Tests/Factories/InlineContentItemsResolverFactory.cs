using System;
using System.Linq;
using KenticoCloud.Delivery.InlineContentItems;

namespace KenticoCloud.Delivery.Tests.Factories
{
    internal static class InlineContentItemsResolverFactory
    {
        public static IInlineContentItemsResolver<HostedVideo> CreateHostedVideoResolver(string messagePrefix)
            => new Resolver<HostedVideo>(messagePrefix, video => video.VideoHost.First().Name);

        public static IInlineContentItemsResolver<Tweet> CreateTweetResolver(string messagePrefix)
            => new Resolver<Tweet>(messagePrefix, tweet => tweet.TweetLink);

        private class Resolver<TContentItem> : IInlineContentItemsResolver<TContentItem>
        {
            private readonly string _messagePrefix;
            private readonly Func<TContentItem, string> _messageSelector;

            public Resolver(string messagePrefix, Func<TContentItem, string> messageSelector)
            {
                this._messagePrefix = messagePrefix;
                this._messageSelector = messageSelector;
            }

            public string Resolve(ResolvedContentItemData<TContentItem> data)
            {
                return _messagePrefix + _messageSelector(data.Item);
            }
        }
    }
}
