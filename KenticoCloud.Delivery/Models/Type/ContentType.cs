using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a content type.
    /// </summary>
    public sealed class ContentType
    {
        /// <summary>
        /// Gets the system attributes of the content type.
        /// </summary>
        public ContentTypeSystemAttributes System { get; }

        /// <summary>
        /// Gets a dictionary that contains elements of the content type index by their codename.
        /// </summary>
        public Dictionary<string, ContentElement> Elements { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentType"/> class with the specified JSON data.
        /// </summary>
        /// <param name="source">The JSON data to deserialize.</param>
        internal ContentType(JToken source)
        {
            System = new ContentTypeSystemAttributes(source["system"]);
            Elements = new Dictionary<string, ContentElement>();

            foreach (JProperty property in source["elements"])
            {
                var element = property.Value;
                var elementType = element["type"].ToString();
                var elementCodename = property.Name;

                switch (elementType)
                {
                    case "multiple_choice":
                        Elements.Add(elementCodename, new MultipleChoiceContentElement(element, elementCodename));
                        break;

                    case "taxonomy":
                        Elements.Add(elementCodename, new TaxonomyContentElement(element, elementCodename));
                        break;

                    default:
                        Elements.Add(elementCodename, new ContentElement(element, elementCodename));
                        break;
                }
            }
        }
    }
}
