using System;
using Refit;

namespace Kontent.Ai.Delivery.Api.QueryParams.Base;

/// <summary>
/// Query parameter for excluding content elements.
/// </summary>
public interface IExcludeElementsParam
{
    /// <summary>
    /// The content elements to exclude in the response. By default, all elements are returned.
    /// </summary>
    [Query(CollectionFormat.Csv)]
    public string[]? ExcludeElements { get; set; }
}
