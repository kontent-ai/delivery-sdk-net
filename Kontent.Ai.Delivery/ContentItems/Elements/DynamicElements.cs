using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Kontent.Ai.Delivery.ContentItems.ElementsConverterFactory;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Represents the elements of a content item in dynamic mode.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DynamicElements"/> class.
/// </remarks>
/// <param name="inner"></param>
[JsonConverter(typeof(DynamicElementsConverter))]
public sealed class DynamicElements(IDictionary<string, JsonElement> inner)
        : ReadOnlyDictionary<string, JsonElement>(inner), IElementsModel
{
}