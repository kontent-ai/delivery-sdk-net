using System;
using System.Linq;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Urls.Delivery.QueryParameters.Filters;

/// <summary>
/// Provides the base class for filter implementations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Filter"/> class.
/// </remarks>
/// <param name="elementOrAttributePath">The codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.</param>
/// <param name="values">The filter values.</param>
public abstract class Filter(string elementOrAttributePath, params string[] values) : IQueryParameter
{
    private static readonly string SEPARATOR = Uri.EscapeDataString(",");

    /// <summary>
    /// Gets the codename of a content element or system attribute, for example <c>elements.title</c> or <c>system.name</c>.
    /// </summary>
    public string ElementOrAttributePath { get; protected set; } = elementOrAttributePath;

    /// <summary>
    /// Gets the filter values.
    /// </summary>
    public string[] Values { get; protected set; } = values;

    /// <summary>
    /// Gets the filter operator.
    /// </summary>
    public string Operator { get; protected set; }

    /// <summary>
    /// Returns the query string representation of the filter.
    /// </summary>
    public string GetQueryStringParameter()
    {
        var escapedValues = Values.Select(Uri.EscapeDataString);
        return $"{Uri.EscapeDataString(ElementOrAttributePath)}{Uri.EscapeDataString(Operator ?? string.Empty)}={string.Join(SEPARATOR, escapedValues)}";
    }
}
