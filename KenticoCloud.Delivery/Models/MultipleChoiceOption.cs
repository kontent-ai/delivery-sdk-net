using Newtonsoft.Json;

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
        [JsonConstructor]
        internal MultipleChoiceOption(string name, string codename)
        {
            Name = name;
            Codename = codename;
        }
    }
}
