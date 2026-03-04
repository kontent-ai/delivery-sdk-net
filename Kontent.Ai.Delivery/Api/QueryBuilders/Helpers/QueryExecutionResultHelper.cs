using System.Net;

namespace Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;

internal static class QueryExecutionResultHelper
{
    public static IDeliveryResult<T> CreateMissingApiResultFailure<T>(string queryType, string queryTarget)
        where T : class
    {
        var error = new Error
        {
            Message = $"Unexpected SDK cache pipeline failure for '{queryType}' query '{queryTarget}': API result was not captured after a cache miss."
        };

        return DeliveryResult.Failure<T>(string.Empty, HttpStatusCode.InternalServerError, error);
    }

    public static IDeliveryResult<T> EnsureApiResult<T>(
        IDeliveryResult<T>? apiResult,
        string queryType,
        string queryTarget)
        where T : class
        => apiResult ?? CreateMissingApiResultFailure<T>(queryType, queryTarget);
}
