using System;
using Kentico.Kontent.Delivery.InlineContentItems;

namespace Kentico.Kontent.Delivery.Tests.DependencyInjectionFrameworks.Helpers
{
    internal class FakeTweetResolver : IInlineContentItemsResolver<Tweet>
    {
        public string Resolve(Tweet data)
            => throw new NotImplementedException();
    }
}
