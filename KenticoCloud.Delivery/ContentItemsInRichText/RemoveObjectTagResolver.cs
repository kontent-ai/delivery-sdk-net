namespace KenticoCloud.Delivery.ContentItemsInRichText
{
    /// <summary>
    /// Resolver which is replacing content items in richtext with empty strings, therefore removing them from text.
    /// </summary>
    public class RemoveObjectTagResolver : IContentItemsInRichTextResolver<object>
    {
        public string Resolve(ResolvedContentItemWrapper<object> item)
        {
            return string.Empty;
        }
    }
}