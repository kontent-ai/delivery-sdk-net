namespace KenticoCloud.Delivery.ContentItemsInRichText
{
    /// <summary>
    /// Resolver which is replacing content items in richtext with warning message about insufficient depth for content item. Used as default for unretrieved content items resolver on Preview environment.
    /// </summary>
    public class ReplaceWithWarningAboutUnretrievedItemResolver : IContentItemsInRichTextResolver<UnretrievedContentItem>
    {
        public string Resolve(ResolvedContentItemData<UnretrievedContentItem> item)
        {
            return "Content item in this richtext was not resolved because it was not retrieved from Delivery API.";
        }
    }
}