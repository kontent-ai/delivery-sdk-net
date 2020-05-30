using System;
using System.Collections;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.StrongTyping;

namespace Kentico.Kontent.Delivery.Tests.DependencyInjectionFrameworks.Helpers
{
    internal class FakeModelProvider : IModelProvider
    {
        public T GetContentItemModel<T>(object item, IEnumerable modularContent) 
            => throw new NotImplementedException();
    }
}
