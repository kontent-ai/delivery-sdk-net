using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a Multiple choice content element.
    /// </summary>
    public sealed class MultipleChoiceContentElement : ContentElement
    {
        /// <summary>
        /// Gets a list of predefined options for the content element.
        /// </summary>
        public IReadOnlyList<MultipleChoiceContentElementOption> Options { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleChoiceContentElement"/> class with the specified JSON data.
        /// </summary>
        /// <param name="source">The JSON data to deserialize.</param>
        /// <param name="codename">The codename of the content element.</param>
        internal MultipleChoiceContentElement(JToken source, string codename) : base(source, codename)
        {
            Options = ((JArray)source["options"]).Select(optionSource => new MultipleChoiceContentElementOption(optionSource)).ToList().AsReadOnly();
        }
    }
}
