namespace KenticoCloud.Delivery.ContentItemsInRichText
{

    /// <summary>
    /// Resolver for unretrieved content items which is replacing content items in richtext with empty strings, therefore removing them from text.
    /// </summary>
    public class ReplaceWithEmptyStringForUnretrievedItemsResolver : IContentItemsInRichTextResolver<UnretrievedContentItem>
    {
        public string Resolve(ResolvedContentItemData<UnretrievedContentItem> item)
        {
            return string.Empty;
        }
    }
}