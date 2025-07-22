using System;
using Refit;

namespace Kontent.Ai.Delivery.Api.QueryParams.Base;

/// <summary>
/// Query parameter for including content elements.
/// </summary>
public interface IElementsParam
{
    /// <summary>
    /// The content elements to include in the response. By default, all elements are returned.
    /// </summary>
    [Query(CollectionFormat.Csv)]
    public string[]? Elements { get; set; }
}