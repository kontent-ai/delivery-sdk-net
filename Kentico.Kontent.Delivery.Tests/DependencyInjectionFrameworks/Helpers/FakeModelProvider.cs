using System;
using System.Collections;
using Kentico.Kontent.Delivery.Abstractions;

namespace Kentico.Kontent.Delivery.Tests.DependencyInjectionFrameworks.Helpers
{
    internal class FakeModelProvider : IModelProvider
    {
        public T GetContentItemModel<T>(object item, IEnumerable modularContent) 
            => throw new NotImplementedException();
    }
}
