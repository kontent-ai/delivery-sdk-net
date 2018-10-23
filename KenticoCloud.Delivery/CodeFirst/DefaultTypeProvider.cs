using System;
using System.Collections.Generic;
using System.Text;

namespace KenticoCloud.Delivery.CodeFirst
{
    internal class DefaultTypeProvider : ICodeFirstTypeProvider
    {
        public Type GetType(string contentType)
            => null;

        public string GetCodename(Type contentType)
            => null;
    }
}
