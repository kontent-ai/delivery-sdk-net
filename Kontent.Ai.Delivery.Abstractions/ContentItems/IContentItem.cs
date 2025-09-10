using System.Collections.Generic;
using System.Text.Json;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a content item with elements.
/// </summary>
/// <typeparam name="TModel">Strongly typed elements of a content item.</typeparam>

public interface IContentItem<out TModel>
    where TModel : IElementsModel
{
    /// <summary>
    /// Represents system attributes of a content item.
    /// </summary>
    public IContentItemSystemAttributes System { get; }

    /// <summary>
    /// Represents the elements of a content item.
    /// </summary>
    public TModel Elements { get; }
}

/// <summary>
/// Represents the elements of a content item.
/// </summary>
public interface IElementsModel { }
