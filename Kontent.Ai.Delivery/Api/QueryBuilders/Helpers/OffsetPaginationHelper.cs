namespace Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;

internal static class OffsetPaginationHelper
{
    internal static int GetNextSkip(IPagination pagination)
    {
        ArgumentNullException.ThrowIfNull(pagination);
        return pagination.Skip + pagination.Count;
    }
}
