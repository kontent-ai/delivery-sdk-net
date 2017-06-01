namespace KenticoCloud.Delivery.InlineContentItems
{
    /// <summary>
    /// Interface which is only used to be extended by it's generic version
    /// </summary>
    public interface IInlineContentItemsResolver
    {
        
    }

    /// <summary>
    /// An interface, implemented to be registered as resolver for specific content type of inline content item
    /// </summary>
    /// <typeparam name="T">Content type to be resolved</typeparam>
    public interface IInlineContentItemsResolver<T> : IInlineContentItemsResolver
    {
        /// <summary>
        /// Method implementing the resolving of inline content item. Result should be valid HTML code
        /// </summary>
        /// <param name="data">Content item to be resolved</param>
        /// <returns>HTML code</returns>
        string Resolve(ResolvedContentItemData<T> data);
    }
}
