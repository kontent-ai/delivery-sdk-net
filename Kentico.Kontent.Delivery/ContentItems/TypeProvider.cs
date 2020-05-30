using System;
using Kentico.Kontent.Delivery.Abstractions.ContentItems;

namespace Kentico.Kontent.Delivery.ContentItems
{
    internal class TypeProvider : ITypeProvider
    {
        public Type GetType(string contentType)
            => null;

        public string GetCodename(Type contentType)
            => null;
    }
}
