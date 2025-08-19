using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentTypes.Element
{
    internal class MultipleChoiceElement : ContentElement, IMultipleChoiceElement
    {
        /// <inheritdoc/>
        [JsonPropertyName("options")]
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
