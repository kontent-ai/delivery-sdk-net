using System;
using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery.Tests.DependencyInjectionFrameworks.Helpers
{
    internal class FakeModelProvider : ICodeFirstModelProvider
    {
        public T GetContentItemModel<T>(JToken item, JToken modularContent) 
            => throw new NotImplementedException();
    }
}
