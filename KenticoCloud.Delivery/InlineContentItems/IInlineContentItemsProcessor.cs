using System.Collections.Generic;

namespace KenticoCloud.Delivery.InlineContentItems
{
    /// <summary>
    /// Interface implemented for processing inline content items in HTML
    /// </summary>
    public interface IInlineContentItemsProcessor
{        /// <summary>
        /// Gets or sets resolver used in case no other resolver was registered for type of inline content item
        /// </summary>
        IInlineContentItemsResolver<object> DefaultResolver { get; set; }

        /// <summary>
        /// Processes HTML input and returns it with inline content items replaced with resolvers output.
        /// </summary>
        /// <param name="value">HTML code</param>
        /// <param name="usedContentItems">Content items referenced as inline content items</param>
        /// <returns>HTML with inline content items replaced with resolvers output</returns>
        string Process(string value, Dictionary<string, object> usedContentItems);

        /// <summary>
        /// Function used for registering content type specific resolvers used during processing.
        /// </summary>
        /// <param name="resolver">Method which is used for specific content type as resolver.</param>
        /// <typeparam name="T">Content type which is resolver resolving.</typeparam>
        void RegisterTypeResolver<T>(IInlineContentItemsResolver<T> resolver);

        /// <summary>
        /// Removes all content items from given HTML content.
        /// </summary>
        /// <param name="value">HTML content</param>
        /// <returns>HTML without inline content items</returns>
        string RemoveAll(string value);
    }
}