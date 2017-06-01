namespace KenticoCloud.Delivery.InlineContentItems
{
    /// <summary>
    /// Resolver which is replacing content items in richtext with empty strings, therefore removing them from text.
    /// </summary>
    public class ReplaceWithEmptyStringResolver : IInlineContentItemsResolver<object>
    {
        /// <summary>
        /// Resolver for inline content items, returning empty string
        /// </summary>
        /// <param name="data">Content item to be resolved</param>
        /// <returns>Empty output</returns>
        public string Resolve(ResolvedContentItemData<object> data)
        {
            return string.Empty;
        }
    }
}