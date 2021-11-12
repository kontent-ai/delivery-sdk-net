using System;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.ContentItems.InlineContentItems;
using Kentico.Kontent.Delivery.Tests.Models.ContentTypes;

namespace Kentico.Kontent.Delivery.Tests.DependencyInjectionFrameworks.Helpers
{
    internal class FakeTweetResolver : IInlineContentItemsResolver<Tweet>
    {
        public string Resolve(Tweet data)
            => throw new NotImplementedException();
    }
}
