using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a content element.
    /// </summary>
    public class ContentElement
    {
        /// <summary>
        /// Gets the type of the content element, for example "multiple_choice".
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Gets the name of the content element.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the codename of the content element.
        /// </summary>
        public string Codename { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentElement"/> class with the specified JSON data.
        /// </summary>
        /// <param name="source">The JSON data to deserialize.</param>
        /// <param name="codename">The codename of the content element.</param>
        internal ContentElement(JToken source, string codename)
        {
            Type = source["type"].ToString();
            Name = source["name"].ToString();
            Codename = codename;
        }
    }
}
