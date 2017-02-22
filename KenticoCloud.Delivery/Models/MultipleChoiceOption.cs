using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a selected option of a Multiple choice element.
    /// </summary>
    public sealed class MultipleChoiceOption
    {
        /// <summary>
        /// Gets the name of the selected option.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the codename of the selected option.
        /// </summary>
        public string Codename { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleChoiceOption"/> class with the specified JSON data.
        /// </summary>
        /// <param name="source">The JSON data to deserialize.</param>
        internal MultipleChoiceOption(JToken source)
        {
            Name = source["name"].ToString();
            Codename = source["codename"].ToString();
        }
    }
}
