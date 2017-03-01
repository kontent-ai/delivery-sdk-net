using KenticoCloud.Delivery;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace KenticoCloud.Delivery.Tests
{
    public class CompleteContentItemModel
    {
        [JsonProperty("text_field")]
        public string TextField { get; set; }

        [JsonProperty("rich_text_field")]
        public string RichTextField { get; set; }

        [JsonProperty("number_field")]
        public decimal? NumberField { get; set; }
        //public IEnumerable<MultipleChoiceOption> MultipleChoiceFieldAsRadioButtons { get; set; }
        //public IEnumerable<MultipleChoiceOption> MultipleChoiceFieldAsCheckboxes { get; set; }

        [JsonProperty("date___time_field")]
        public DateTime? DateTimeField { get; set; }

        [JsonProperty("asset_field")]
        public IEnumerable<Asset> AssetField { get; set; }
        //public IEnumerable<ContentItem> ModularContentField { get; set; }
        //public IEnumerable<TaxonomyTerm> CompleteTypeTaxonomy { get; set; }
    }
}