namespace KenticoCloud.Delivery.InlineContentItems
{
    /// <summary>
    /// Resolver for unretrieved content items which is replacing content items in richtext with empty strings, therefore removing them from text.
    /// </summary>
    public class ReplaceWithEmptyStringForUnretrievedItemsResolver : IInlineContentItemsResolver<UnretrievedContentItem>
    {
        /// <summary>
        /// Resolver for unretrieved inline content items, returning empty string
        /// </summary>
        /// <param name="data">Unretrieved content item</param>
        /// <returns>Empty output</returns>
        public string Resolve(ResolvedContentItemData<UnretrievedContentItem> data)
        {
            return string.Empty;
        }
    }
}