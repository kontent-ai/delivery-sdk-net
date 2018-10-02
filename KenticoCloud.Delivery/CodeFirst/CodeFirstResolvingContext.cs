using System;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Context of the current resolving process
    /// </summary>
    public class CodeFirstResolvingContext
    {
        /// <summary>
        /// Gets the content item within current resolving context
        /// </summary>
        public Func<string, object> GetModularContentItem { get; internal set; }

        public IContentLinkUrlResolver ContentLinkUrlResolver { get; internal set; }
    }
}
