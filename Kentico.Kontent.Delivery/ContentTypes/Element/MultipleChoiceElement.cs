using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.ContentTypes.Element
{
    internal class MultipleChoiceElement : ContentElement, IMultipleChoiceElement
    {
        /// <inheritdoc/>
        [JsonProperty("options")]
        public IList<IMultipleChoiceOption> Options { get; internal set; }

        /// <summary>
        /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
        /// </summary>
        [JsonConstructor]
        public MultipleChoiceElement()
        {
        }
    }
}
