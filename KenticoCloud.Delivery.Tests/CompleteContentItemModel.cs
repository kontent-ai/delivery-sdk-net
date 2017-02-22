using KenticoCloud.Delivery;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery.Tests
{
    public class CompleteContentItemModel : IContentItemBased
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
        public ItemSystem System { get; set; }

        public void LoadFromContentItem(ContentItem contentItem)
        {
            TextField = contentItem.GetString("text_field");
            RichTextField = contentItem.GetString("rich_text_field");
            NumberField = contentItem.GetNumber("number_field");
            MultipleChoiceFieldAsRadioButtons = contentItem.GetOptions("multiple_choice_field_as_radio_buttons");
            MultipleChoiceFieldAsCheckboxes = contentItem.GetOptions("multiple_choice_field_as_checkboxes");
            DateTimeField = contentItem.GetDateTime("date___time_field");
            AssetField = contentItem.GetAssets("asset_field");
            ModularContentField = contentItem.GetModularContent("modular_content_field");
            CompleteTypeTaxonomy = contentItem.GetTaxonomyTerms("complete_type_taxonomy");
            System = contentItem.System;
        }
    }
}