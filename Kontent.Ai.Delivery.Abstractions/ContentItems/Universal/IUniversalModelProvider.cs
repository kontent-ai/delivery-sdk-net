using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions
{
    public interface IUniversalItemModelProvider
    {
        public Task<IUniversalContentItem> GetContentItemGenericModelAsync(object item);
    }
}