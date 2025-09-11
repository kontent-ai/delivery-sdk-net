using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Post-processes deserialized content items to hydrate advanced element types
    /// such as rich text blocks using original element JSON and modular content.
    /// </summary>
    public interface IElementsPostProcessor
    {
        /// <summary>
        /// Hydrates advanced element types on a strongly typed content item.
        /// </summary>
        /// <typeparam name="TModel">Elements model type.</typeparam>
        /// <param name="item">The content item to process.</param>
        /// <param name="modularContent">Raw modular content dictionary from the API response.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ProcessAsync<TModel>(
            IContentItem<TModel> item,
            IReadOnlyDictionary<string, JsonElement>? modularContent,
            CancellationToken cancellationToken = default)
            where TModel : IElementsModel;
    }
}


