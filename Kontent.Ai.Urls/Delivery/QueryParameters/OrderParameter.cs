using System;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Urls.Delivery.QueryParameters;

/// <summary>
/// Specifies how content items are sorted.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OrderParameter"/> class using the specified element or system attribute and sorting order.
/// </remarks>
/// <param name="elementOrAttributePath">The codename of a content element or system attribute by which content items are sorted, for example <c>elements.title</c> or <c>system.name</c>.</param>
/// <param name="sortOrder">The order in which the content items are sorted.</param>
public sealed class OrderParameter(string elementOrAttributePath, SortOrder sortOrder = SortOrder.Ascending) : IQueryParameter
{
    /// <summary>
    /// Gets the order in which content items are sorted.
    /// </summary>
    public SortOrder SortOrder { get; } = sortOrder;

    /// <summary>
    /// Gets the codename of a content element or system attribute by which content items are sorted.
    /// </summary>
    public string ElementOrAttributePath { get; } = elementOrAttributePath;

    /// <summary>
    /// Returns the query string representation of the query parameter.
    /// </summary>
    public string GetQueryStringParameter()
    {
        return $"order={Uri.EscapeDataString(ElementOrAttributePath)}{Uri.EscapeDataString(SortOrder == SortOrder.Ascending ? "[asc]" : "[desc]")}";
    }
}
