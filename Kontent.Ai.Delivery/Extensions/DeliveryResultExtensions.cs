using Kontent.Ai.Delivery.Abstractions.SharedModels;

namespace Kontent.Ai.Delivery.Extensions;

internal static class DeliveryResultExtensions
{
    public static IDeliveryResult<TOut> Map<TIn, TOut>(this IDeliveryResult<TIn> result, Func<TIn, TOut> map)
    {
        if (result.IsSuccess)
        {
            var mapped = map(result.Value);
            return DeliveryResult.Success(
                mapped,
                result.StatusCode,
                result.HasStaleContent,
                result.ContinuationToken,
                result.RequestUrl,
                result.RateLimit);
        }

        return DeliveryResult.Failure<TOut>(
            result.Errors,
            result.StatusCode,
            result.RequestUrl,
            result.RateLimit);
    }

    public static async Task<IDeliveryResult<TOut>> MapAsync<TIn, TOut>(this IDeliveryResult<TIn> result, Func<TIn, Task<TOut>> mapAsync)
    {
        if (result.IsSuccess)
        {
            var mapped = await mapAsync(result.Value);
            return DeliveryResult.Success(
                mapped,
                result.StatusCode,
                result.HasStaleContent,
                result.ContinuationToken,
                result.RequestUrl,
                result.RateLimit);
        }

        return DeliveryResult.Failure<TOut>(
            result.Errors,
            result.StatusCode,
            result.RequestUrl,
            result.RateLimit);
    }

    public static async Task<IDeliveryResult<TOut>> MapTryAsync<TIn, TOut>(this IDeliveryResult<TIn> result, Func<TIn, Task<TOut>> mapAsync, Func<Exception, string> errorMessage)
    {
        if (!result.IsSuccess)
        {
            return DeliveryResult.Failure<TOut>(
                result.Errors,
                result.StatusCode,
                result.RequestUrl,
                result.RateLimit);
        }

        try
        {
            var mapped = await mapAsync(result.Value);
            return DeliveryResult.Success(
                mapped,
                result.StatusCode,
                result.HasStaleContent,
                result.ContinuationToken,
                result.RequestUrl,
                result.RateLimit);
        }
        catch (Exception ex)
        {
            return DeliveryResult.Failure<TOut>(
                errorMessage(ex),
                result.StatusCode,
                requestUrl: result.RequestUrl,
                rateLimit: result.RateLimit);
        }
    }
}


