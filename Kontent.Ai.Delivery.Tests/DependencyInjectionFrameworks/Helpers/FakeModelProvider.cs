using System;
using System.Collections;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Tests.DependencyInjectionFrameworks.Helpers
{
    internal class FakeModelProvider : IModelProvider
    {
        public Task<T> GetContentItemModelAsync<T>(object item, IEnumerable modularContent)
            => throw new NotImplementedException();
    }
}
