namespace KenticoCloud.Delivery.ContentItemsInRichText
{
    /// <summary>
    /// Resolver which is replacing content items in richtext with empty strings, therefore removing them from text.
    /// </summary>
    public class ReplaceWithEmptyStringResolver : IInlineContentItemsResolver<object>
    {
        public string Resolve(ResolvedContentItemData<object> item)
        {
            return string.Empty;
        }
    }
}