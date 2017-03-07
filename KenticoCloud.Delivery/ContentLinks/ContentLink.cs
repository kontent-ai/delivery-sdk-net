using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    // Represents a link to a content item in Rich text element values.
    public sealed class ContentLink
    {
        // Gets the properties of the linked content item.
        public ContentLinkTarget Target
        {
            get;
        }

        internal ContentLink(string id, JToken source)
        {
            Target = new ContentLinkTarget(id, source);
        }
    }
}
