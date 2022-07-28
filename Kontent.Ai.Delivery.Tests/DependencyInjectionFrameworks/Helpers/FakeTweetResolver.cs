using System;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;

namespace Kontent.Ai.Delivery.Tests.DependencyInjectionFrameworks.Helpers
{
    internal class FakeTweetResolver : IInlineContentItemsResolver<Tweet>
    {
        public string Resolve(Tweet data)
            => throw new NotImplementedException();
    }
}
