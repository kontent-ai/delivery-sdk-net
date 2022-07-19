using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Interface implemented for processing inline content items in HTML
    /// </summary>
    public interface IInlineContentItemsProcessor
    {
        /// <summary>
        /// Processes HTML input and returns it with inline content items replaced with resolvers output.
        /// </summary>
        /// <param name="value">HTML code</param>
        /// <param name="usedContentItems">Content items referenced as inline content items</param>
        /// <returns>HTML with inline content items replaced with resolvers output</returns>
        Task<string> ProcessAsync(string value, Dictionary<string, object> usedContentItems);

        /// <summary>
        /// Removes all content items from given HTML content.
        /// </summary>
        /// <param name="value">HTML content</param>
        /// <returns>HTML without inline content items</returns>
        Task<string> RemoveAllAsync(string value);
    }
}