using System;
using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.Tests.Models
{
    public class ContentItemModelWithAttributes
    {
        [JsonPropertyName("text_field")]
        public string TextFieldWithADifferentName { get; set; }

        [JsonPropertyName("rich_text_field")]
        public string RichTextFieldWithADifferentName { get; set; }

        [JsonPropertyName("number_field")]
        public decimal? NumberFieldWithADifferentName { get; set; }

        [JsonPropertyName("multiple_choice_field_as_radio_buttons")]
        public IEnumerable<IMultipleChoiceOption> MultipleChoiceFieldAsRadioButtonsWithADifferentName { get; set; }

        [JsonPropertyName("multiple_choice_field_as_checkboxes")]
        public IEnumerable<IMultipleChoiceOption> MultipleChoiceFieldAsCheckboxes { get; set; }

        [JsonPropertyName("date___time_field")]
        public DateTime? DateTimeFieldWithADifferentName { get; set; }

        [JsonPropertyName("asset_field")]
        public IEnumerable<IAsset> AssetFieldWithADifferentName { get; set; }

        [JsonPropertyName("linked_items_field")]
        public IEnumerable<object> LinkedItemsFieldWithADifferentName { get; set; }

        [JsonPropertyName("linked_items_field")]
        public HashSet<object> LinkedItemsFieldWithACollectionTypeDefined { get; set; }

        public IEnumerable<string> RandomField { get; set; }

        [JsonPropertyName("linked_items_field")]
        public HashSet<Homepage> LinkedItemsFieldWithAGenericTypeDefined { get; set; }

        [JsonPropertyName("complete_type_taxonomy")]
        public IEnumerable<ITaxonomyTerm> CompleteTypeTaxonomyWithADifferentName { get; set; }

        public IContentItemSystemAttributes System { get; set; }
    }
}