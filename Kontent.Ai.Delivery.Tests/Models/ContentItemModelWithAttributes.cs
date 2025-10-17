using System;
using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.SharedModels;
using Kontent.Ai.Delivery.ContentItems;

namespace Kontent.Ai.Delivery.Tests.Models;

public class ContentItemModelWithAttributes
{
    [JsonPropertyName("text_field")]
    public string TextFieldWithADifferentName { get; set; }

    [JsonPropertyName("rich_text_field")]
    public string RichTextFieldWithADifferentName { get; set; }

    [JsonPropertyName("number_field")]
    public decimal? NumberFieldWithADifferentName { get; set; }

    [JsonPropertyName("multiple_choice_field_as_radio_buttons")]
    public IEnumerable<MultipleChoiceOption> MultipleChoiceFieldAsRadioButtonsWithADifferentName { get; set; }

    [JsonPropertyName("multiple_choice_field_as_checkboxes")]
    public IEnumerable<MultipleChoiceOption> MultipleChoiceFieldAsCheckboxes { get; set; }

    [JsonPropertyName("date___time_field")]
    public DateTime? DateTimeFieldWithADifferentName { get; set; }

    [JsonPropertyName("asset_field")]
    public IEnumerable<Asset> AssetFieldWithADifferentName { get; set; }

    [JsonPropertyName("linked_items_field")]
    public IEnumerable<string> LinkedItemsFieldWithADifferentName { get; set; }

    [JsonPropertyName("linked_items_field")]
    public HashSet<object> LinkedItemsFieldWithACollectionTypeDefined { get; set; }

    public IEnumerable<string> RandomField { get; set; }

    [JsonPropertyName("linked_items_field")]
    public HashSet<Homepage> LinkedItemsFieldWithAGenericTypeDefined { get; set; }

    [JsonPropertyName("complete_type_taxonomy")]
    public IEnumerable<TaxonomyTerm> CompleteTypeTaxonomyWithADifferentName { get; set; }

    public IContentItemSystemAttributes System { get; set; }
}