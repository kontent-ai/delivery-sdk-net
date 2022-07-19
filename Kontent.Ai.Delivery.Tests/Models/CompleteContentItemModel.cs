using System;
using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Tests.Models
{
    public class CompleteContentItemModel
    {
        public string TextField { get; set; }
        public string RichTextField { get; set; }
        public decimal? NumberField { get; set; }
        public IEnumerable<IMultipleChoiceOption> MultipleChoiceFieldAsRadioButtons { get; set; }
        public IEnumerable<IMultipleChoiceOption> MultipleChoiceFieldAsCheckboxes { get; set; }
        public DateTime? DateTimeField { get; set; }
        public IEnumerable<IAsset> AssetField { get; set; }
        public IEnumerable<Homepage> LinkedItemsField { get; set; }
        public IEnumerable<ITaxonomyTerm> CompleteTypeTaxonomy { get; set; }
        public string CustomElementField { get; set; }
        public IContentItemSystemAttributes System { get; set; }
    }
}