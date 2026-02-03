using System.Collections.ObjectModel;
using System.Text.Json;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Represents the elements of a content item in dynamic mode.
/// Deserialization is handled by ContentItemConverterFactory.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DynamicElements"/> class.
/// </remarks>
/// <param name="inner">Dictionary of element names to JsonElement values.</param>
public sealed class DynamicElements(IDictionary<string, JsonElement> inner)
        : ReadOnlyDictionary<string, JsonElement>(inner), IDynamicElements
{
}
