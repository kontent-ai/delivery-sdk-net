using System;
using System.Collections.Generic;
using System.Linq;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Urls.Delivery.QueryParameters;

/// <summary>
/// Specifies the content elements to retrieve.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ElementsParameter"/> class using the specified content elements codenames.
/// </remarks>
/// <param name="elementCodenames">An array that contains zero or more codenames of the content elements that should be retrieved.</param>
public sealed class ElementsParameter(params string[] elementCodenames) : IQueryParameter
{
    /// <summary>
    /// Gets the list of codenames of content elements that should be retrieved.
    /// </summary>
    public IReadOnlyList<string> ElementCodenames { get; } = elementCodenames.ToList().AsReadOnly();

    /// <summary>
    /// Returns the query string representation of the query parameter.
    /// </summary>
    public string GetQueryStringParameter()
    {
        return $"elements={string.Join(Uri.EscapeDataString(","), ElementCodenames.Select(Uri.EscapeDataString))}";
    }
}
