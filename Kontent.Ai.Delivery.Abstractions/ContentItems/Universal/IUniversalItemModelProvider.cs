using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Interface ensuring mapping dynamic response to <see cref="IUniversalContentItem"/>.
    /// </summary>
    public interface IUniversalItemModelProvider
    {
        /// <summary>
        /// Builds a model based on given JSON input.
        /// </summary>
        /// <param name="item">Content item data.</param>
        /// <returns>Universal item</returns>
        public Task<IUniversalContentItem> GetUniversalContentItemModelAsync(object item);
    }
}
