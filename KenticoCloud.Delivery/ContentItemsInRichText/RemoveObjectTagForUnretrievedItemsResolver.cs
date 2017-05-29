namespace KenticoCloud.Delivery.ContentItemsInRichText
{

    /// <summary>
    /// Resolver for unretrieved content items which is replacing content items in richtext with empty strings, therefore removing them from text.
    /// </summary>
    public class RemoveObjectTagForUnretrievedItemsResolver : IContentItemsInRichTextResolver<UnretrievedContentItem>
    {
        public string Resolve(ResolvedContentItemWrapper<UnretrievedContentItem> item)
        {
            return string.Empty;
        }
    }
}