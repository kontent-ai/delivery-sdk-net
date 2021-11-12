using System;
using System.Collections;
using System.Threading.Tasks;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.ContentItems;

namespace Kentico.Kontent.Delivery.Tests.DependencyInjectionFrameworks.Helpers
{
    internal class FakeModelProvider : IModelProvider
    {
        public Task<T> GetContentItemModelAsync<T>(object item, IEnumerable modularContent)
            => throw new NotImplementedException();
    }
}
