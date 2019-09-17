using System;
using Newtonsoft.Json.Linq;

namespace KenticoKontent.Delivery.Tests.DependencyInjectionFrameworks.Helpers
{
    internal class FakeModelProvider : IModelProvider
    {
        public T GetContentItemModel<T>(JToken item, JToken modularContent) 
            => throw new NotImplementedException();
    }
}
