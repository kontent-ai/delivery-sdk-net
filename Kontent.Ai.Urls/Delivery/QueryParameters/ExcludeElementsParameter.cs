using System;
using System.Collections.Generic;
using System.Linq;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Urls.Delivery.QueryParameters;

/// <summary>
/// Specifies the content elements to exclude from response.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExcludeElementsParameter"/> class using the specified content elements codenames.
/// </remarks>
/// <param name="elementCodenames">An array that contains zero or more codenames of the content elements that should be excluded from response.</param>
public sealed class ExcludeElementsParameter(params string[] elementCodenames) : IQueryParameter
{
    /// <summary>
    /// Gets the list of codenames of content elements that should be excluded from response.
    /// </summary>
    public IReadOnlyList<string> ElementCodenames { get; } = elementCodenames.ToList().AsReadOnly();

    /// <summary>
    /// Returns the query string representation of the query parameter.
    /// </summary>
    public string GetQueryStringParameter()
    {
        return $"excludeElements={string.Join(Uri.EscapeDataString(","), ElementCodenames.Select(Uri.EscapeDataString))}";
    }
}