using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents an option of a multiple choice element.
    /// </summary>
    public class Option
    {
        /// <summary>
        /// Gets or sets the name of the option.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the codename of the option.
        /// </summary>
        public string Codename { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Option"/> class with the specified JSON data.
        /// </summary>
        /// <param name="option">The JSON data to deserialize.</param>
        public Option(JToken option)
        {
            Name = option["name"].ToString();
            Codename = option["codename"].ToString();
        }
    }
}