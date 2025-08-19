using System.Text.Json;
using Kontent.Ai.Delivery.Abstractions.Serialization;
using Kontent.Ai.Delivery.Abstractions.SharedModels;
using Kontent.Ai.Delivery.Api.ResponseModels;
using Kontent.Ai.Delivery.Extensions;
using Kontent.Ai.Delivery.SharedModels;
using Refit;
using System.Net.Http;
using Kontent.Ai.Delivery.ContentTypes;
using Kontent.Ai.Delivery.TaxonomyGroups;
using Kontent.Ai.Delivery.Languages;
using Kontent.Ai.Delivery.UsedIn;

namespace Kontent.Ai.Delivery.Services;

/// <summary>
/// Service responsible for processing raw Refit API responses into strongly-typed delivery results.
/// </summary>
internal sealed class DeliveryResponseProcessor
{
    private readonly IModelProvider _modelProvider;
    private readonly IJsonSerializer _jsonSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryResponseProcessor"/> class.
    /// </summary>
    /// <param name="modelProvider">The model provider for creating strongly-typed objects.</param>
    /// <param name="jsonSerializer">The JSON serializer for parsing content.</param>
    public DeliveryResponseProcessor(IModelProvider modelProvider, IJsonSerializer jsonSerializer)
    {
        _modelProvider = modelProvider ?? throw new ArgumentNullException(nameof(modelProvider));
        _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
    }

    /// <summary>
    /// Processes a single content type response.
    /// </summary>
    public async Task<IDeliveryResult<IDeliveryTypeResponse>> ProcessTypeResponseAsync(
        IApiResponse<RawContentTypeResponse> apiResponse)
    {
        var baseResult = await apiResponse.ToDeliveryResultAsync(_jsonSerializer);
        return baseResult.Map(raw =>
        {
            var typeJson = raw.Type.ToString() ?? string.Empty;
            var contentType = _jsonSerializer.Deserialize<IContentType>(typeJson);
            return new DeliveryTypeResponse(
                new ProcessedApiResponse(baseResult.HasStaleContent, baseResult.ContinuationToken, baseResult.RequestUrl, baseResult.StatusCode),
                contentType);
        });
    }

    /// <summary>
    /// Processes a multiple content types response.
    /// </summary>
    public async Task<IDeliveryResult<IDeliveryTypeListingResponse>> ProcessTypesListingResponseAsync(
        IApiResponse<RawContentTypeListingResponse> apiResponse)
    {
        var baseResult = await apiResponse.ToDeliveryResultAsync(_jsonSerializer);
        return baseResult.Map(raw =>
        {
            var types = new List<IContentType>();
            foreach (var t in raw.Types)
            {
                var json = t.ToString() ?? string.Empty;
                var type = _jsonSerializer.Deserialize<IContentType>(json);
                if (type != null)
                {
                    types.Add(type);
                }
            }

            var pagination = ConvertPagination(raw.Pagination);
            return new DeliveryTypeListingResponse(
                new ProcessedApiResponse(baseResult.HasStaleContent, baseResult.ContinuationToken, baseResult.RequestUrl, baseResult.StatusCode),
                types,
                pagination);
        });
    }

    /// <summary>
    /// Processes a content element response.
    /// </summary>
    public async Task<IDeliveryResult<IDeliveryElementResponse>> ProcessContentElementResponseAsync(
        IApiResponse<RawContentElementResponse> apiResponse)
    {
        var baseResult = await apiResponse.ToDeliveryResultAsync(_jsonSerializer);
        return baseResult.Map(raw =>
        {
            var elementJson = raw.Element.ToString() ?? string.Empty;
            var element = _jsonSerializer.Deserialize<IContentElement>(elementJson);
            return new DeliveryElementResponse(
                new ProcessedApiResponse(baseResult.HasStaleContent, baseResult.ContinuationToken, baseResult.RequestUrl, baseResult.StatusCode),
                element);
        });
    }

    /// <summary>
    /// Processes a single taxonomy response.
    /// </summary>
    public async Task<IDeliveryResult<IDeliveryTaxonomyResponse>> ProcessTaxonomyResponseAsync(
        IApiResponse<RawTaxonomyResponse> apiResponse)
    {
        var baseResult = await apiResponse.ToDeliveryResultAsync(_jsonSerializer);
        return baseResult.Map(raw =>
        {
            var taxonomyJson = raw.Taxonomy.ToString() ?? string.Empty;
            var taxonomy = _jsonSerializer.Deserialize<ITaxonomyGroup>(taxonomyJson);
            return new DeliveryTaxonomyResponse(
                new ProcessedApiResponse(baseResult.HasStaleContent, baseResult.ContinuationToken, baseResult.RequestUrl, baseResult.StatusCode),
                taxonomy);
        });
    }

    /// <summary>
    /// Processes a taxonomy listing response.
    /// </summary>
    public async Task<IDeliveryResult<IDeliveryTaxonomyListingResponse>> ProcessTaxonomyListingResponseAsync(
        IApiResponse<RawTaxonomyListingResponse> apiResponse)
    {
        var baseResult = await apiResponse.ToDeliveryResultAsync(_jsonSerializer);
        return baseResult.Map(raw =>
        {
            var taxonomies = new List<ITaxonomyGroup>();
            foreach (var t in raw.Taxonomies)
            {
                var json = t.ToString() ?? string.Empty;
                var taxonomy = _jsonSerializer.Deserialize<ITaxonomyGroup>(json);
                if (taxonomy != null)
                {
                    taxonomies.Add(taxonomy);
                }
            }

            var pagination = ConvertPagination(raw.Pagination);
            return new DeliveryTaxonomyListingResponse(
                new ProcessedApiResponse(baseResult.HasStaleContent, baseResult.ContinuationToken, baseResult.RequestUrl, baseResult.StatusCode),
                taxonomies,
                pagination);
        });
    }

    /// <summary>
    /// Processes a languages listing response.
    /// </summary>
    public async Task<IDeliveryResult<IDeliveryLanguageListingResponse>> ProcessLanguageListingResponseAsync(
        IApiResponse<RawLanguageListingResponse> apiResponse)
    {
        var baseResult = await apiResponse.ToDeliveryResultAsync(_jsonSerializer);
        return baseResult.Map(raw =>
        {
            var languages = new List<ILanguage>();
            foreach (var l in raw.Languages)
            {
                var json = l.ToString() ?? string.Empty;
                var language = _jsonSerializer.Deserialize<ILanguage>(json);
                if (language != null)
                {
                    languages.Add(language);
                }
            }

            var pagination = ConvertPagination(raw.Pagination);
            return new DeliveryLanguageListingResponse(
                new ProcessedApiResponse(baseResult.HasStaleContent, baseResult.ContinuationToken, baseResult.RequestUrl, baseResult.StatusCode),
                languages,
                pagination);
        });
    }

    /// <summary>
    /// Processes a single content item response.
    /// </summary>
    /// <typeparam name="T">The target type for the content item.</typeparam>
    /// <param name="apiResponse">The raw API response from Refit.</param>
    /// <returns>A delivery result containing the processed content item.</returns>
    public async Task<IDeliveryResult<IDeliveryItemResponse<T>>> ProcessItemResponseAsync<T>(
        IApiResponse<RawContentItemResponse> apiResponse)
    {
        var baseResult = await apiResponse.ToDeliveryResultAsync(_jsonSerializer);
        return await baseResult.MapTryAsync(async raw =>
        {
            var contentItem = await _modelProvider.GetContentItemModelAsync<T>(
                raw.Item,
                raw.ModularContent ?? new Dictionary<string, object>());
            return new ProcessedDeliveryItemResponse<T>(
                contentItem,
                baseResult.HasStaleContent,
                baseResult.ContinuationToken,
                baseResult.RequestUrl,
                baseResult.StatusCode);
        }, ex => $"Failed to process content item: {ex.Message}");
    }

    /// <summary>
    /// Processes a multiple content items response.
    /// </summary>
    /// <typeparam name="T">The target type for the content items.</typeparam>
    /// <param name="apiResponse">The raw API response from Refit.</param>
    /// <returns>A delivery result containing the processed content items.</returns>
    public async Task<IDeliveryResult<IDeliveryItemListingResponse<T>>> ProcessItemListingResponseAsync<T>(
        IApiResponse<RawContentItemListingResponse> apiResponse)
    {
        var baseResult = await apiResponse.ToDeliveryResultAsync(_jsonSerializer);
        return await baseResult.MapTryAsync(async raw =>
        {
            var items = new List<T>();
            foreach (var rawItem in raw.Items)
            {
                var contentItem = await _modelProvider.GetContentItemModelAsync<T>(
                    rawItem,
                    raw.ModularContent ?? new Dictionary<string, object>());
                items.Add(contentItem);
            }
            var pagination = ConvertPagination(raw.Pagination);
            return new ProcessedDeliveryItemListingResponse<T>(
                items,
                pagination,
                baseResult.HasStaleContent,
                baseResult.ContinuationToken,
                baseResult.RequestUrl,
                baseResult.StatusCode);
        }, ex => $"Failed to process content items: {ex.Message}");
    }

    /// <summary>
    /// Processes a content items feed response.
    /// </summary>
    /// <typeparam name="T">The target type for the content items.</typeparam>
    /// <param name="apiResponse">The raw API response from Refit.</param>
    /// <returns>A delivery result containing the processed content items feed.</returns>
    public async Task<IDeliveryResult<IDeliveryItemsFeedResponse<T>>> ProcessItemsFeedResponseAsync<T>(
        IApiResponse<RawContentItemsFeedResponse> apiResponse)
    {
        var baseResult = await apiResponse.ToDeliveryResultAsync(_jsonSerializer);
        return await baseResult.MapTryAsync(async raw =>
        {
            var items = new List<T>();
            foreach (var rawItem in raw.Items)
            {
                var contentItem = await _modelProvider.GetContentItemModelAsync<T>(
                    rawItem,
                    raw.ModularContent ?? new Dictionary<string, object>());
                items.Add(contentItem);
            }
            return new ProcessedDeliveryItemsFeedResponse<T>(
                items,
                baseResult.HasStaleContent,
                baseResult.ContinuationToken,
                baseResult.RequestUrl,
                baseResult.StatusCode);
        }, ex => $"Failed to process items feed: {ex.Message}");
    }

    /// <summary>
    /// Converts raw pagination to the expected pagination interface.
    /// </summary>
    /// <param name="rawPagination">The raw pagination from the API.</param>
    /// <returns>The converted pagination interface.</returns>
    private static IPagination ConvertPagination(RawPagination? rawPagination)
    {
        if (rawPagination == null)
        {
            return new ProcessedPagination(0, 0, 0, null, null);
        }

        return new ProcessedPagination(
            rawPagination.Skip,
            rawPagination.Limit,
            rawPagination.Count,
            rawPagination.NextPage,
            rawPagination.TotalCount);
    }

    /// <summary>
    /// Processes a used-in response (items referencing an item or asset).
    /// </summary>
    public async Task<IDeliveryResult<IDeliveryItemsFeedResponse<IUsedInItem>>> ProcessUsedInResponseAsync(
        IApiResponse<RawUsedInResponse> apiResponse)
    {
        var baseResult = await apiResponse.ToDeliveryResultAsync(_jsonSerializer);
        if (!baseResult.IsSuccess)
        {
            return DeliveryResult.Failure<IDeliveryItemsFeedResponse<IUsedInItem>>(
                baseResult.Errors,
                baseResult.StatusCode,
                baseResult.RequestUrl,
                baseResult.RateLimit);
        }

        try
        {
            var raw = baseResult.Value;
            var items = new List<IUsedInItem>();
            foreach (var i in raw.Items)
            {
                var json = i.ToString() ?? string.Empty;
                var item = _jsonSerializer.Deserialize<IUsedInItem>(json);
                if (item != null)
                {
                    items.Add(item);
                }
            }

            var envelope = new DeliveryUsedInResponse(
                new ProcessedApiResponse(baseResult.HasStaleContent, baseResult.ContinuationToken, baseResult.RequestUrl, baseResult.StatusCode),
                items);

            return DeliveryResult.Success<IDeliveryItemsFeedResponse<IUsedInItem>>(
                envelope,
                baseResult.StatusCode,
                baseResult.HasStaleContent,
                baseResult.ContinuationToken,
                baseResult.RequestUrl,
                baseResult.RateLimit);
        }
        catch (Exception ex)
        {
            return DeliveryResult.Failure<IDeliveryItemsFeedResponse<IUsedInItem>>(
                $"Failed to process used-in response: {ex.Message}",
                baseResult.StatusCode,
                requestUrl: baseResult.RequestUrl);
        }
    }
}

/// <summary>
/// Processed implementation of <see cref="IDeliveryItemResponse{T}"/> using the Result pattern.
/// </summary>
/// <typeparam name="T">The type of the content item.</typeparam>
internal sealed class ProcessedDeliveryItemResponse<T> : IDeliveryItemResponse<T>
{
    /// <inheritdoc/>
    public T Item { get; }

    /// <inheritdoc/>
    public Kontent.Ai.Delivery.Abstractions.IApiResponse ApiResponse { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessedDeliveryItemResponse{T}"/> class.
    /// </summary>
    /// <param name="item">The processed content item.</param>
    /// <param name="hasStaleContent">Whether the content is stale.</param>
    /// <param name="continuationToken">The continuation token.</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    public ProcessedDeliveryItemResponse(
        T item,
        bool hasStaleContent,
        string? continuationToken,
        string? requestUrl,
        int statusCode)
    {
        Item = item;
        ApiResponse = new ProcessedApiResponse(hasStaleContent, continuationToken, requestUrl, statusCode);
    }
}

/// <summary>
/// Processed implementation of <see cref="IDeliveryItemListingResponse{T}"/> using the Result pattern.
/// </summary>
/// <typeparam name="T">The type of the content items.</typeparam>
internal sealed class ProcessedDeliveryItemListingResponse<T> : IDeliveryItemListingResponse<T>
{
    /// <inheritdoc/>
    public IList<T> Items { get; }

    /// <inheritdoc/>
    public IPagination Pagination { get; }

    /// <inheritdoc/>
    public Kontent.Ai.Delivery.Abstractions.IApiResponse ApiResponse { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessedDeliveryItemListingResponse{T}"/> class.
    /// </summary>
    /// <param name="items">The processed content items.</param>
    /// <param name="pagination">The pagination information.</param>
    /// <param name="hasStaleContent">Whether the content is stale.</param>
    /// <param name="continuationToken">The continuation token.</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    public ProcessedDeliveryItemListingResponse(
        IList<T> items,
        IPagination pagination,
        bool hasStaleContent,
        string? continuationToken,
        string? requestUrl,
        int statusCode)
    {
        Items = items;
        Pagination = pagination;
        ApiResponse = new ProcessedApiResponse(hasStaleContent, continuationToken, requestUrl, statusCode);
    }
}

/// <summary>
/// Processed implementation of <see cref="IDeliveryItemsFeedResponse{T}"/> using the Result pattern.
/// </summary>
/// <typeparam name="T">The type of the content items.</typeparam>
internal sealed class ProcessedDeliveryItemsFeedResponse<T> : IDeliveryItemsFeedResponse<T>
{
    /// <inheritdoc/>
    public IList<T> Items { get; }

    /// <inheritdoc/>
    public Kontent.Ai.Delivery.Abstractions.IApiResponse ApiResponse { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessedDeliveryItemsFeedResponse{T}"/> class.
    /// </summary>
    /// <param name="items">The processed content items.</param>
    /// <param name="hasStaleContent">Whether the content is stale.</param>
    /// <param name="continuationToken">The continuation token.</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    public ProcessedDeliveryItemsFeedResponse(
        IList<T> items,
        bool hasStaleContent,
        string? continuationToken,
        string? requestUrl,
        int statusCode)
    {
        Items = items;
        ApiResponse = new ProcessedApiResponse(hasStaleContent, continuationToken, requestUrl, statusCode);
    }
}

/// <summary>
/// Processed implementation of <see cref="Kontent.Ai.Delivery.Abstractions.IApiResponse"/> for the new Result pattern.
/// </summary>
internal sealed class ProcessedApiResponse : Kontent.Ai.Delivery.Abstractions.IApiResponse
{
    /// <inheritdoc/>
    public string Content { get; }

    /// <inheritdoc/>
    public string ContinuationToken { get; }

    /// <inheritdoc/>
    public bool HasStaleContent { get; }

    /// <inheritdoc/>
    public string RequestUrl { get; }

    /// <inheritdoc/>
    public bool IsSuccess { get; }

    /// <inheritdoc/>
    public IError Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessedApiResponse"/> class.
    /// </summary>
    /// <param name="hasStaleContent">Whether the content is stale.</param>
    /// <param name="continuationToken">The continuation token.</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    public ProcessedApiResponse(
        bool hasStaleContent,
        string? continuationToken,
        string? requestUrl,
        int statusCode)
    {
        HasStaleContent = hasStaleContent;
        ContinuationToken = continuationToken ?? string.Empty;
        RequestUrl = requestUrl ?? string.Empty;
        Content = string.Empty; // Not used in the new pattern
        IsSuccess = statusCode >= 200 && statusCode < 300;
        Error = null!; // Will be null for successful responses
    }
}

/// <summary>
/// Processed implementation of <see cref="IPagination"/>.
/// </summary>
internal sealed class ProcessedPagination : IPagination
{
    /// <inheritdoc/>
    public int Skip { get; }

    /// <inheritdoc/>
    public int Limit { get; }

    /// <inheritdoc/>
    public int Count { get; }

    /// <inheritdoc/>
    public string NextPageUrl { get; }

    /// <inheritdoc/>
    public int? TotalCount { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessedPagination"/> class.
    /// </summary>
    /// <param name="skip">The number of items to skip.</param>
    /// <param name="limit">The maximum number of items to return.</param>
    /// <param name="count">The actual number of items returned.</param>
    /// <param name="nextPageUrl">The URL for the next page.</param>
    /// <param name="totalCount">The total number of items available.</param>
    public ProcessedPagination(int skip, int limit, int count, string? nextPageUrl, int? totalCount)
    {
        Skip = skip;
        Limit = limit;
        Count = count;
        NextPageUrl = nextPageUrl ?? string.Empty;
        TotalCount = totalCount;
    }
}