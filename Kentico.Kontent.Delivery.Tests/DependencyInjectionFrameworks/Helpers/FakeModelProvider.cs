using System;
using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.Tests.DependencyInjectionFrameworks.Helpers
{
    internal class FakeModelProvider : IModelProvider
    {
        public T GetContentItemModel<T>(JToken item, JToken modularContent) 
            => throw new NotImplementedException();
    }
}
