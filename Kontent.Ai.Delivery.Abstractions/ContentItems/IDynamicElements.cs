using System.Text.Json;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents the elements of a content item when using dynamic (untyped) content retrieval.
/// Provides dictionary-style access to raw JSON element values.
/// </summary>
public interface IDynamicElements : IElementsModel, IReadOnlyDictionary<string, JsonElement>
{
}
