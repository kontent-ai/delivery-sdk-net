using System;
using System.Collections.Generic;

namespace KenticoKontent.Delivery.Tests
{
    public class CompleteContentItemModel
    {
        public string TextField { get; set; }
        public string RichTextField { get; set; }
        public decimal? NumberField { get; set; }
        public IEnumerable<MultipleChoiceOption> MultipleChoiceFieldAsRadioButtons { get; set; }
        public IEnumerable<MultipleChoiceOption> MultipleChoiceFieldAsCheckboxes { get; set; }
        public DateTime? DateTimeField { get; set; }
        public IEnumerable<Asset> AssetField { get; set; }
        public IEnumerable<ContentItem> LinkedItemsField { get; set; }
        public IEnumerable<TaxonomyTerm> CompleteTypeTaxonomy { get; set; }
        public string CustomElementField { get; set; }
        public ContentItemSystemAttributes System { get; set; }
    }
}