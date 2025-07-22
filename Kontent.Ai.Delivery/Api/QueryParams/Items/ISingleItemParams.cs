using Kontent.Ai.Delivery.Api.QueryParams.Base;

namespace Kontent.Ai.Delivery.Api.QueryParams.Items;

/// <summary>
/// Query parameters for a single content item.
/// </summary>
public interface ISingleItemParams :
    ILanguageParam,
    IElementsParam,
    IDepthParam
{
}