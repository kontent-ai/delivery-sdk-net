namespace KenticoCloud.Delivery.ContentItemsInRichText
{
    /// <summary>
    /// Resolver which is replacing content items in richtext with warning message about insufficient depth for content item. Used as default for unretrieved content items resolver on Preview environment.
    /// </summary>
    public class ReplaceWithWarningAboutDepthResolver : IContentItemsInRichTextResolver<UnretrievedContentItem>
    {
        public string Resolve(ResolvedContentItemWrapper<UnretrievedContentItem> item)
        {
            return "Content item in this richtext was not resolved because depth of your request was insufficient for this content item to be retrieved.";
        }
    }
}