namespace Kontent.Ai.Delivery.Extensions;

internal static class DeliveryResultExtensions
{
    /// <summary>
    /// Maps the result value to a new type. Used to convert deserialized response to a simplified domain model (e.g. from IDeliveryItemResponse to IContentItem).
    /// </summary>
    /// <typeparam name="TIn">The type of the input result value.</typeparam>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="map">The function to map the input result value to the output result value.</param>
    /// <returns>A new result with the mapped value.</returns>
    public static IDeliveryResult<TOut> Map<TIn, TOut>(this IDeliveryResult<TIn> result, Func<TIn, TOut> map)
    {
        if (result.IsSuccess)
        {
            var mapped = map(result.Value);
            return DeliveryResult.Success(
                mapped,
                result.RequestUrl ?? string.Empty,
                result.StatusCode,
                result.HasStaleContent,
                result.ContinuationToken);
        }

        return DeliveryResult.Failure<TOut>(
            result.RequestUrl ?? string.Empty,
            result.StatusCode,
            result.Error);
    }
}


