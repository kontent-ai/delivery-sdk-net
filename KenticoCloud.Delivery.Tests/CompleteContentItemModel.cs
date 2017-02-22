using KenticoCloud.Delivery;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery.Tests
{
    public class CompleteContentItemModel : ContentItem
    {
        public string TextField { get; set; }
        public string RichTextField { get; set; }
        public decimal? NumberField { get; set; }
        public List<Option> MultipleChoiceFieldAsRadioButtons { get; set; }
        public List<Option> MultipleChoiceFieldAsCheckboxes { get; set; }
        public DateTime? DateTimeField { get; set; }
        public List<Asset> AssetField { get; set; }
        public IEnumerable<ContentItem> ModularContentField { get; set; }
        public List<TaxonomyTerm> CompleteTypeTaxonomy { get; set; }

        public override void MapElementsFromJson(JToken item, JToken modularContent)
        {
            base.MapElementsFromJson(item, modularContent);

            TextField = GetString("text_field");
            RichTextField = GetString("rich_text_field");
            NumberField = GetNumber("number_field");
            MultipleChoiceFieldAsRadioButtons = GetOptions("multiple_choice_field_as_radio_buttons");
            MultipleChoiceFieldAsCheckboxes = GetOptions("multiple_choice_field_as_checkboxes");
            DateTimeField = GetDateTime("date___time_field");
            AssetField = GetAssets("asset_field");
            ModularContentField = GetModularContent("modular_content_field");
            CompleteTypeTaxonomy = GetTaxonomyTerms("complete_type_taxonomy");
        }
    }
}