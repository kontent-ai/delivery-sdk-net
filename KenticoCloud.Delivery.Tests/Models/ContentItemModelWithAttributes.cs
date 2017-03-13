using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace KenticoCloud.Delivery.Tests
{
    public class ContentItemModelWithAttributes
    {
        [JsonProperty("text_field")]
        public string TextFieldWithADifferentName { get; set; }

        [JsonProperty("rich_text_field")]
        public string RichTextFieldWithADifferentName { get; set; }

        [JsonProperty("number_field")]
        public decimal? NumberFieldWithADifferentName { get; set; }

        [JsonProperty("multiple_choice_field_as_radio_buttons")]
        public IEnumerable<MultipleChoiceOption> MultipleChoiceFieldAsRadioButtonsWithADifferentName { get; set; }

        [JsonProperty("multiple_choice_field_as_checkboxes")]
        public IEnumerable<MultipleChoiceOption> MultipleChoiceFieldAsCheckboxes { get; set; }

        [JsonProperty("date___time_field")]
        public DateTime? DateTimeFieldWithADifferentName { get; set; }

        [JsonProperty("asset_field")]
        public IEnumerable<Asset> AssetFieldWithADifferentName { get; set; }

        [JsonProperty("modular_content_field")]
        public IEnumerable<object> ModularContentFieldWithADifferentName { get; set; }

        [JsonProperty("modular_content_field")]
        public HashSet<object> ModularContentFieldWithACollectionTypeDefined { get; set; }

        public IEnumerable<string> RandomField { get; set; }

        [JsonProperty("modular_content_field")]
        public HashSet<Homepage> ModularContentFieldWithAGenericTypeDefined { get; set; }

        [JsonProperty("complete_type_taxonomy")]
        public IEnumerable<TaxonomyTerm> CompleteTypeTaxonomyWithADifferentName { get; set; }
        
        public ContentItemSystemAttributes System { get; set; }
    }
}