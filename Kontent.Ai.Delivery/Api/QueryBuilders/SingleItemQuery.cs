using System.Threading;
using Kontent.Ai.Delivery.Abstractions.QueryBuilders;
using Kontent.Ai.Delivery.Abstractions.Serialization;
using Kontent.Ai.Delivery.Abstractions.SharedModels;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <summary>
/// Concrete implementation of <see cref="ISingleItemQuery{T}"/> using the modernized Result pattern.
/// </summary>
/// <typeparam name="T">The type of the content item.</typeparam>
internal sealed class SingleItemQuery<T>(
    IDeliveryApi api, 
    string codename, 
    DeliveryResponseProcessor responseProcessor) : ISingleItemQuery<T>
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private readonly DeliveryResponseProcessor _responseProcessor = responseProcessor;
    private SingleItemParams _params = new();

    public ISingleItemQuery<T> WithLanguage(string languageCodename)
    {
        _params = _params with { Language = languageCodename };
        return this;
    }

    public ISingleItemQuery<T> WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public ISingleItemQuery<T> WithoutElements(params string[] elementCodenames)
    {
        _params = _params with { ExcludeElements = elementCodenames };
        return this;
    }

    public ISingleItemQuery<T> Depth(int depth)
    {
        _params = _params with { Depth = depth };
        return this;
    }

    public async Task<IDeliveryResult<T>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Get raw response from Refit API
        var rawResponse = await _api.GetItemInternalAsync(_codename, _params, null);
        
        // Process the response to create strongly-typed result
        var processedResponse = await _responseProcessor.ProcessItemResponseAsync<T>(rawResponse);
        
        // Extract the content item from the processed response
        if (processedResponse.IsSuccess)
        {
            return DeliveryResult.Success(
                processedResponse.Value.Item,
                processedResponse.StatusCode,
                processedResponse.HasStaleContent,
                processedResponse.ContinuationToken,
                processedResponse.RequestUrl,
                processedResponse.RateLimit);
        }

        // Return error result
        return DeliveryResult.Failure<T>(
            processedResponse.Errors,
            processedResponse.StatusCode,
            processedResponse.RequestUrl,
            processedResponse.RateLimit);
    }
}