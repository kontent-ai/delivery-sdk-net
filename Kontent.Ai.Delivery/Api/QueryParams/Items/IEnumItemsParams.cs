using Kontent.Ai.Delivery.Api.QueryParams.Base;

namespace Kontent.Ai.Delivery.Api.QueryParams.Items;

/// <summary>
/// Query parameters for enumerating content items.
/// </summary>
public interface IEnumItemsParams :
    ILanguageParam,
    IElementsParam,
    IExcludeElementsParam,
    IDepthParam,
    IOrderingParam
{
}