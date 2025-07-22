using Kontent.Ai.Delivery.Api.QueryParams.Base;

namespace Kontent.Ai.Delivery.Api.QueryParams.Items;

/// <summary>
/// Query parameters for content item listing.
/// </summary>
public interface IListItemsParams :
    ILanguageParam,
    IElementsParam,
    IExcludeElementsParam,
    IDepthParam,
    IPagingParams,
    IOrderingParam,
    IIncludeTotalCountParam
{
}