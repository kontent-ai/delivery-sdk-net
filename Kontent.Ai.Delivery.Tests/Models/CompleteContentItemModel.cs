using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.Tests.Models;

public record CompleteContentItemModel : IElementsModel
{
    [JsonPropertyName("text_field")]
    public string TextField { get; init; }
    [JsonPropertyName("rich_text_field")]
    public string RichTextField { get; init; }
    [JsonPropertyName("number_field")]
    public decimal? NumberField { get; init; }
    [JsonPropertyName("multiple_choice_field_as_radio_buttons")]
    public IEnumerable<MultipleChoiceOption> MultipleChoiceFieldAsRadioButtons { get; init; }
    [JsonPropertyName("multiple_choice_field_as_checkboxes")]
    public IEnumerable<MultipleChoiceOption> MultipleChoiceFieldAsCheckboxes { get; init; }
    [JsonPropertyName("date___time_field")]
    public DateTime? DateTimeField { get; init; }
    [JsonPropertyName("asset_field")]
    public IEnumerable<Asset> AssetField { get; init; }
    [JsonPropertyName("linked_items_field")]
    public IEnumerable<Homepage> LinkedItemsField { get; init; }
    [JsonPropertyName("complete_type_taxonomy")]
    public IEnumerable<TaxonomyTerm> CompleteTypeTaxonomy { get; init; }
    [JsonPropertyName("custom_element_field")]
    public string CustomElementField { get; init; }
    [JsonPropertyName("system")]
    public IContentItemSystemAttributes System { get; init; }
}