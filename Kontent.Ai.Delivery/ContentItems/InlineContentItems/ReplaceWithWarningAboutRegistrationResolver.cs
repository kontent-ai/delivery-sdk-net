using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.ContentItems.InlineContentItems
{
    /// <summary>
    /// /// Resolver which is replacing content items in richtext with warning message about content type resolver not being registered. Used as default for default resolver on Preview environment.
    /// </summary>
    internal class ReplaceWithWarningAboutRegistrationResolver : IInlineContentItemsResolver<object>
    {
        /// <inheritdoc />
        public string Resolve(object item)
            => $"Resolver for content type {item.GetType()} is not registered. Please do so in your app.";
    }
}