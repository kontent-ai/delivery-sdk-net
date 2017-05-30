namespace KenticoCloud.Delivery.ContentItemsInRichText
{
    /// <summary>
    /// /// Resolver which is replacing content items in richtext with warning message about content type resolver not being registered. Used as default for default resolver on Preview environment.
    /// </summary>
    public class ReplaceWithWarningAboutRegistrationResolver : IContentItemsInRichTextResolver<object>
    {
        public string Resolve(ResolvedContentItemData<object> item)
        {
            return $"Resolver for content type {item.GetType()} is not registered. Please do so in your app.";
        }
    }
}