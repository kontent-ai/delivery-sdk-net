﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentTypes;
using Kontent.Ai.Delivery.Extensions;
using Kontent.Ai.Delivery.Languages;
using Kontent.Ai.Delivery.SharedModels;
using Kontent.Ai.Delivery.Sync;
using Kontent.Ai.Delivery.TaxonomyGroups;
using Kontent.Ai.Urls.Delivery;
using Kontent.Ai.Urls.Delivery.QueryParameters;
using Kontent.Ai.Urls.Delivery.QueryParameters.Filters;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kontent.Ai.Delivery
{
    /// <summary>
    /// Executes requests against the Kontent.ai Delivery API.
    /// </summary>
    internal sealed class DeliveryClient : IDeliveryClient
    {
        private DeliveryEndpointUrlBuilder _urlBuilder;

        internal readonly IOptionsMonitor<DeliveryOptions> DeliveryOptions;
        internal readonly IModelProvider ModelProvider;
        internal readonly ITypeProvider TypeProvider;
        internal readonly IRetryPolicyProvider RetryPolicyProvider;
        internal readonly IDeliveryHttpClient DeliveryHttpClient;
        internal readonly JsonSerializer Serializer;
        internal readonly ILoggerFactory LoggerFactory;

        internal DeliveryEndpointUrlBuilder UrlBuilder
            => _urlBuilder ??= new DeliveryEndpointUrlBuilder(DeliveryOptions);

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryClient"/> class for retrieving content of the specified project.
        /// </summary>
        /// <param name="deliveryOptions">The settings of the Kontent.ai project.</param>
        /// <param name="modelProvider">An instance of an object that can JSON responses into strongly typed CLR objects</param>
        /// <param name="retryPolicyProvider">A provider of a retry policy.</param>
        /// <param name="typeProvider">An instance of an object that can map Kontent.ai content types to CLR types</param>
        /// <param name="deliveryHttpClient">An instance of an object that can send request against Kontent.ai Delivery API</param>
        /// <param name="serializer">Default JSON serializer</param>
        /// <param name="loggerFactory">The factory used to create loggers</param>
        public DeliveryClient(
            IOptionsMonitor<DeliveryOptions> deliveryOptions,
            IModelProvider modelProvider = null,
            IRetryPolicyProvider retryPolicyProvider = null,
            ITypeProvider typeProvider = null,
            IDeliveryHttpClient deliveryHttpClient = null,
            JsonSerializer serializer = null,
            ILoggerFactory loggerFactory = null)
        {
            DeliveryOptions = deliveryOptions;
            ModelProvider = modelProvider;
            RetryPolicyProvider = retryPolicyProvider;
            TypeProvider = typeProvider;
            DeliveryHttpClient = deliveryHttpClient;
            Serializer = serializer;
            LoggerFactory = loggerFactory;
        }

        /// <summary>
        /// Gets a strongly typed content item by its codename. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example, for projection or setting the depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemResponse{T}"/> instance that contains the content item with the specified codename.</returns>
        public async Task<IDeliveryItemResponse<T>> GetItemAsync<T>(string codename, IEnumerable<IQueryParameter> parameters = null)
        {
            if (string.IsNullOrEmpty(codename))
            {
                throw new ArgumentException("Entered item codename is not valid.", nameof(codename));
            }

            var endpointUrl = UrlBuilder.GetItemUrl(codename, parameters);
            var response = await GetDeliveryResponseAsync(endpointUrl, HttpMethod.Get);
            
            if (!response.IsSuccess)
            {
                return new DeliveryItemResponse<T>(response);
            }

            var content = await response.GetJsonContentAsync();
            var model = await ModelProvider.GetContentItemModelAsync<T>(content?["item"], content?["modular_content"]);
            return new DeliveryItemResponse<T>(response, model);
        }

        /// <summary>
        /// Returns strongly typed content items that match the optional filtering parameters. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">A collection of query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse{T}"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<IDeliveryItemListingResponse<T>> GetItemsAsync<T>(IEnumerable<IQueryParameter> parameters = null)
        {
            var enhancedParameters = EnsureContentTypeFilter<T>(parameters).ToList();
            var endpointUrl = UrlBuilder.GetItemsUrl(enhancedParameters);
            var response = await GetDeliveryResponseAsync(endpointUrl, HttpMethod.Get);
            
            if (!response.IsSuccess)
            {
                return new DeliveryItemListingResponse<T>(response);
            }
            
            var content = await response.GetJsonContentAsync();
            var pagination = content["pagination"].ToObject<Pagination>(Serializer);
            var items = ((JArray)content["items"]).Select(async source => await ModelProvider.GetContentItemModelAsync<T>(source, content["modular_content"]));

            return new DeliveryItemListingResponse<T>(response, (await Task.WhenAll(items)).ToList(), pagination);
        }

        /// <summary>
        /// Returns a feed that is used to traverse through strongly typed content items matching the optional filtering parameters.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">A collection of query parameters, for example, for filtering or ordering.</param>
        /// <returns>The <see cref="DeliveryItemsFeed{T}"/> instance that can be used to enumerate through content items. If no query parameters are specified, all content items are enumerated.</returns>
        public IDeliveryItemsFeed<T> GetItemsFeed<T>(IEnumerable<IQueryParameter> parameters = null)
        {
            var enhancedParameters = EnsureContentTypeFilter<T>(parameters).ToList();
            ValidateItemsFeedParameters(enhancedParameters);
            var endpointUrl = UrlBuilder.GetItemsFeedUrl(enhancedParameters);
            return new DeliveryItemsFeed<T>(GetItemsBatchAsync);

            async Task<DeliveryItemsFeedResponse<T>> GetItemsBatchAsync(string continuationToken)
            {
                var response = await GetDeliveryResponseAsync(endpointUrl, HttpMethod.Get, continuationToken);
                
                if (!response.IsSuccess)
                {
                    return new DeliveryItemsFeedResponse<T>(response);
                }
                
                var content = await response.GetJsonContentAsync();

                var items = ((JArray)content["items"]).Select(async source => await ModelProvider.GetContentItemModelAsync<T>(source, content["modular_content"]));

                return new DeliveryItemsFeedResponse<T>(response, (await Task.WhenAll(items)).ToList());
            }
        }

        /// <summary>
        /// Gets a content type by its codename.
        /// </summary>
        /// <param name="codename">The codename of a content type.</param>
        /// <returns>The <see cref="DeliveryTypeResponse"/> instance that contains the content type with the specified codename.</returns>
        public async Task<IDeliveryTypeResponse> GetTypeAsync(string codename)
        {
            if (codename == null)
            {
                throw new ArgumentNullException(nameof(codename), "The codename of a content type is not specified.");
            }

            if (codename == string.Empty)
            {
                throw new ArgumentException("The codename of a content type is not specified.", nameof(codename));
            }

            var endpointUrl = UrlBuilder.GetTypeUrl(codename);
            var response = await GetDeliveryResponseAsync(endpointUrl, HttpMethod.Get);

            if (!response.IsSuccess)
            {
                return new DeliveryTypeResponse(response);
            }

            var type = (await response.GetJsonContentAsync())?.ToObject<ContentType>(Serializer);

            return new DeliveryTypeResponse(response, type);
        }

        /// <summary>
        /// Returns content types that match the optional filtering parameters.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for paging.</param>
        /// <returns>The <see cref="DeliveryTypeListingResponse"/> instance that contains the content types. If no query parameters are specified, all content types are returned.</returns>
        public async Task<IDeliveryTypeListingResponse> GetTypesAsync(IEnumerable<IQueryParameter> parameters = null)
        {
            var endpointUrl = UrlBuilder.GetTypesUrl(parameters);
            var response = await GetDeliveryResponseAsync(endpointUrl, HttpMethod.Get);

            if (!response.IsSuccess)
            {
                return new DeliveryTypeListingResponse(response);
            }
            
            var content = await response.GetJsonContentAsync();
            var pagination = content["pagination"].ToObject<Pagination>(Serializer);
            var types = content["types"].ToObject<List<ContentType>>(Serializer);
            return new DeliveryTypeListingResponse(response, types.ToList<IContentType>(), pagination);
        }

        /// <summary>
        /// Returns a content type element.
        /// </summary>
        /// <param name="contentTypeCodename">The codename of the content type.</param>
        /// <param name="contentElementCodename">The codename of the content type element.</param>
        /// <returns>The <see cref="DeliveryElementResponse"/> instance that contains the specified content type element.</returns>
        public async Task<IDeliveryElementResponse> GetContentElementAsync(string contentTypeCodename, string contentElementCodename)
        {
            if (contentTypeCodename == null)
            {
                throw new ArgumentNullException(nameof(contentTypeCodename), "The codename of a content type is not specified.");
            }

            if (contentTypeCodename == string.Empty)
            {
                throw new ArgumentException("The codename of a content type is not specified.", nameof(contentTypeCodename));
            }

            if (contentElementCodename == null)
            {
                throw new ArgumentNullException(nameof(contentElementCodename), "The codename of a content element is not specified.");
            }

            if (contentElementCodename == string.Empty)
            {
                throw new ArgumentException("The codename of a content element is not specified.", nameof(contentElementCodename));
            }

            var endpointUrl = UrlBuilder.GetContentElementUrl(contentTypeCodename, contentElementCodename);
            var response = await GetDeliveryResponseAsync(endpointUrl, HttpMethod.Get);

            if (!response.IsSuccess)
            {
                return new DeliveryElementResponse(response);
            }

            var content = await response.GetJsonContentAsync();
            var element = content?.ToObject<IContentElement>(Serializer);
            return new DeliveryElementResponse(response, element);
        }

        /// <summary>
        /// Returns a taxonomy group.
        /// </summary>
        /// <param name="codename">The codename of a taxonomy group.</param>
        /// <returns>The <see cref="DeliveryTaxonomyResponse"/> instance that contains the taxonomy group with the specified codename.</returns>
        public async Task<IDeliveryTaxonomyResponse> GetTaxonomyAsync(string codename)
        {
            if (codename == null)
            {
                throw new ArgumentNullException(nameof(codename), "The codename of a taxonomy group is not specified.");
            }

            if (codename == string.Empty)
            {
                throw new ArgumentException("The codename of a taxonomy group is not specified.", nameof(codename));
            }

            var endpointUrl = UrlBuilder.GetTaxonomyUrl(codename);
            var response = await GetDeliveryResponseAsync(endpointUrl, HttpMethod.Get);

            if (!response.IsSuccess)
            {
                return new DeliveryTaxonomyResponse(response);
            }

            var taxonomy = (await response.GetJsonContentAsync())?.ToObject<TaxonomyGroup>(Serializer);
            return new DeliveryTaxonomyResponse(response, taxonomy);
        }

        /// <summary>
        /// Returns taxonomy groups.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for paging.</param>
        /// <returns>The <see cref="DeliveryTaxonomyListingResponse"/> instance that represents the taxonomy groups. If no query parameters are specified, all taxonomy groups are returned.</returns>
        public async Task<IDeliveryTaxonomyListingResponse> GetTaxonomiesAsync(IEnumerable<IQueryParameter> parameters = null)
        {
            var endpointUrl = UrlBuilder.GetTaxonomiesUrl(parameters);
            var response = await GetDeliveryResponseAsync(endpointUrl, HttpMethod.Get);
            
            if (!response.IsSuccess)
            {
                return new DeliveryTaxonomyListingResponse(response);
            }
            
            var content = await response.GetJsonContentAsync();
            var pagination = content["pagination"].ToObject<Pagination>(Serializer);
            var taxonomies = content["taxonomies"].ToObject<List<TaxonomyGroup>>(Serializer);
            return new DeliveryTaxonomyListingResponse(response, taxonomies.ToList<ITaxonomyGroup>(), pagination);
        }

        /// <summary>
        /// Returns all active languages assigned to a given project and matching the optional filtering parameters.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for paging.</param>
        /// <returns>The <see cref="DeliveryLanguageListingResponse"/> instance that represents the languages. If no query parameters are specified, all languages are returned.</returns>
        public async Task<IDeliveryLanguageListingResponse> GetLanguagesAsync(IEnumerable<IQueryParameter> parameters = null)
        {
            var endpointUrl = UrlBuilder.GetLanguagesUrl(parameters);
            var response = await GetDeliveryResponseAsync(endpointUrl, HttpMethod.Get);
            
            if (!response.IsSuccess)
            {
                return new DeliveryLanguageListingResponse(response);
            }
            
            var content = await response.GetJsonContentAsync();
            var pagination = content["pagination"].ToObject<Pagination>(Serializer);
            var languages = content["languages"].ToObject<List<Language>>(Serializer);
            return new DeliveryLanguageListingResponse(response, languages.ToList<ILanguage>(), pagination);
        }

        public async Task<IDeliverySyncInitResponse> PostSyncInitAsync(IEnumerable<IQueryParameter> parameters = null)
        {
            var endpointUrl = UrlBuilder.GetSyncInitUrl(parameters);
            var response = await GetDeliveryResponseAsync(endpointUrl, httpMethod: HttpMethod.Post);

            if (!response.IsSuccess)
            {
                return new DeliverySyncInitResponse(response);
            }

            var content = await response.GetJsonContentAsync();
            var items = content["items"].ToObject<List<SyncItem>>(Serializer);
            return new DeliverySyncInitResponse(response, items.ToList<ISyncItem>());
        }

        public async Task<IDeliverySyncResponse> GetSyncAsync(string continuationToken)
        {
            var endpointUrl = UrlBuilder.GetSyncUrl();
            var response = await GetDeliveryResponseAsync(endpointUrl, HttpMethod.Get, continuationToken);

            if (!response.IsSuccess)
            {
                return new DeliverySyncResponse(response);
            }

            var content = await response.GetJsonContentAsync();
            var items = content["items"].ToObject<List<SyncItem>>(Serializer);
            return new DeliverySyncResponse(response, items.ToList<ISyncItem>());
        }

        private async Task<ApiResponse> GetDeliveryResponseAsync(string endpointUrl, HttpMethod httpMethod, string continuationToken = null)
        {
            if (DeliveryOptions.CurrentValue.UsePreviewApi && DeliveryOptions.CurrentValue.UseSecureAccess)
            {
                throw new InvalidOperationException("Preview API and Production API with secured access enabled can't be used at the same time.");
            }

            if (DeliveryOptions.CurrentValue.EnableRetryPolicy)
            {
                var retryPolicy = RetryPolicyProvider.GetRetryPolicy();
                if (retryPolicy != null)
                {
                    var response = await retryPolicy.ExecuteAsync(() => SendHttpMessageAsync(endpointUrl, httpMethod, continuationToken));
                    return await GetResponseContentAsync(response, endpointUrl);
                }
            }

            // Omit using the resilience logic completely.
            return await GetResponseContentAsync(await SendHttpMessageAsync(endpointUrl, httpMethod, continuationToken), endpointUrl);
        }

        private Task<HttpResponseMessage> SendHttpMessageAsync(string endpointUrl, HttpMethod httpMethod, string continuationToken = null)
        {
            var message = new HttpRequestMessage(httpMethod, endpointUrl);

            if (DeliveryOptions.CurrentValue.WaitForLoadingNewContent)
            {
                message.Headers.AddWaitForLoadingNewContentHeader();
            }

            if (UseSecureAccess())
            {
                message.Headers.AddAuthorizationHeader("Bearer", DeliveryOptions.CurrentValue.SecureAccessApiKey);
            }

            if (UsePreviewApi())
            {
                message.Headers.AddAuthorizationHeader("Bearer", DeliveryOptions.CurrentValue.PreviewApiKey);
            }

            if (continuationToken != null)
            {
                message.Headers.AddContinuationHeader(continuationToken);
            }

            return DeliveryHttpClient.SendHttpMessageAsync(message);
        }

        private bool UseSecureAccess()
        {
            return DeliveryOptions.CurrentValue.UseSecureAccess && !string.IsNullOrEmpty(DeliveryOptions.CurrentValue.SecureAccessApiKey);
        }

        private bool UsePreviewApi()
        {
            return DeliveryOptions.CurrentValue.UsePreviewApi && !string.IsNullOrEmpty(DeliveryOptions.CurrentValue.PreviewApiKey);
        }

        private async Task<ApiResponse> GetResponseContentAsync(HttpResponseMessage httpResponseMessage, string fallbackEndpointUrl)
        {
            if (httpResponseMessage == null) throw new ArgumentNullException(nameof(httpResponseMessage));
            if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
            {
                var hasStaleContent = HasStaleContent(httpResponseMessage);
                var continuationToken = httpResponseMessage.Headers.GetContinuationHeader();
                var requestUri = httpResponseMessage.RequestMessage?.RequestUri?.AbsoluteUri ?? fallbackEndpointUrl;

                return new ApiResponse(httpResponseMessage.Content, hasStaleContent, continuationToken, requestUri);
            }

            Error error = null;

            // The null-coalescing operator causes tests to fail for NREs, hence the "if" statement.
            if (httpResponseMessage?.Content != null)
            {
                using var streamReader = new HttpRequestStreamReader(await httpResponseMessage.Content.ReadAsStreamAsync(), Encoding.UTF8);
                using var jsonReader = new JsonTextReader(streamReader);
                error = Serializer.Deserialize<Error>(jsonReader);
            }

            if (error != null)
            {
                return new ApiResponse(httpResponseMessage.Content, false, null, null, error);
            }

            throw new DeliveryException(httpResponseMessage);
        }

        private static bool HasStaleContent(HttpResponseMessage httpResponseMessage)
        {
            return httpResponseMessage.Headers.TryGetValues("X-Stale-Content", out var values) && values.Contains("1", StringComparer.Ordinal);
        }

        internal IEnumerable<IQueryParameter> EnsureContentTypeFilter<T>(IEnumerable<IQueryParameter> parameters = null)
        {
            List<IQueryParameter> enhancedParameters = parameters?.ToList() ?? new List<IQueryParameter>();

            var codename = TypeProvider.GetCodename(typeof(T));

            if (!string.IsNullOrEmpty(codename) && !IsTypeInQueryParameters(enhancedParameters))
            {
                enhancedParameters.Add(new SystemTypeEqualsFilter(codename));
            }
            return enhancedParameters;
        }

        private static bool IsTypeInQueryParameters(IEnumerable<IQueryParameter> parameters)
        {
            var typeFilterExists = parameters?
                .OfType<EqualsFilter>()
                .Any(filter => filter
                    .ElementOrAttributePath
                    .Equals("system.type", StringComparison.Ordinal));
            return typeFilterExists ?? false;
        }

        private static void ValidateItemsFeedParameters(IEnumerable<IQueryParameter> parameters)
        {
            var parameterList = parameters.ToList();
            if (parameterList.Any(x => x is DepthParameter))
            {
                throw new ArgumentException("Depth parameter is not supported in items feed.");
            }

            if (parameterList.Any(x => x is LimitParameter))
            {
                throw new ArgumentException("Limit parameter is not supported in items feed.");
            }

            if (parameterList.Any(x => x is SkipParameter))
            {
                throw new ArgumentException("Skip parameter is not supported in items feed.");
            }
        }
    }
}