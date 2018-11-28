using System;
using KenticoCloud.Delivery.InlineContentItems;

namespace KenticoCloud.Delivery.Tests.DependencyInjectionFrameworks.Helpers
{
    internal class FakeTweetResolver : IInlineContentItemsResolver<Tweet>
    {
        public string Resolve(ResolvedContentItemData<Tweet> data)
            => throw new NotImplementedException();
    }
}
