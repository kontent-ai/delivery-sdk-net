using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using KenticoKontent.Delivery.Extensions;
using KenticoKontent.Delivery.InlineContentItems;
using KenticoKontent.Delivery.ResiliencePolicy;

namespace KenticoKontent.Delivery
{
    /// <summary>
    /// Executes requests against the Kentico Kontent Delivery API.
    /// </summary>
    internal sealed class DeliveryClient : IDeliveryClient
    {
        internal readonly DeliveryOptions DeliveryOptions;
        internal readonly IContentLinkUrlResolver ContentLinkUrlResolver;
        internal readonly IInlineContentItemsProcessor InlineContentItemsProcessor;
        internal readonly IModelProvider ModelProvider;
        internal readonly ITypeProvider TypeProvider;
        internal readonly IPropertyMapper PropertyMapper;
        internal readonly IResiliencePolicyProvider ResiliencePolicyProvider;
        internal readonly HttpClient HttpClient;

        private DeliveryEndpointUrlBuilder _urlBuilder;

        private DeliveryEndpointUrlBuilder UrlBuilder 
            => _urlBuilder ?? (_urlBuilder = new DeliveryEndpointUrlBuilder(DeliveryOptions));

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryClient"/> class for retrieving content of the specified project.
        /// </summary>
        /// <param name="deliveryOptions">The settings of the Kentico Kontent project.</param>
        /// <param name="httpClient">A custom HTTP client instance</param>
        /// <param name="contentLinkUrlResolver">An instance of an object that can resolve links in rich text elements</param>
        /// <param name="contentItemsProcessor">An instance of an object that can resolve linked items in rich text elements</param>
        /// <param name="modelProvider">An instance of an object that can JSON responses into strongly typed CLR objects</param>
        /// <param name="retryPolicyProvider">A provider of a resilience (retry) policy.</param>
        /// <param name="typeProvider">An instance of an object that can map Kentico Kontent content types to CLR types</param>
        /// <param name="propertyMapper">An instance of an object that can map Kentico Kontent content item fields to model properties</param>
        public DeliveryClient(
            IOptions<DeliveryOptions> deliveryOptions,
            HttpClient httpClient = null,
            IContentLinkUrlResolver contentLinkUrlResolver = null,
            IInlineContentItemsProcessor contentItemsProcessor = null,
            IModelProvider modelProvider = null,
            IResiliencePolicyProvider retryPolicyProvider = null,
            ITypeProvider typeProvider = null,
            IPropertyMapper propertyMapper = null
        )
        {
            DeliveryOptions = deliveryOptions.Value;
            HttpClient = httpClient;
            ContentLinkUrlResolver = contentLinkUrlResolver;
            InlineContentItemsProcessor = contentItemsProcessor;
            ModelProvider = modelProvider;
            ResiliencePolicyProvider = retryPolicyProvider;
            TypeProvider = typeProvider;
            PropertyMapper = propertyMapper;
        }

        /// <summary>
        /// Returns a content item as JSON data. By default, retrieves one level of modular content.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for projection or setting the depth of linked items.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content item with the specified codename.</returns>
        public async Task<JObject> GetItemJsonAsync(string codename, params string[] parameters)
        {
            if (codename == null)
            {
                throw new ArgumentNullException(nameof(codename), "The codename of a content item is not specified.");
            }

            if (codename == string.Empty)
            {
                throw new ArgumentException("The codename of a content item is not specified.", nameof(codename));
            }

            var endpointUrl = UrlBuilder.GetItemUrl(codename, parameters);

            return await GetDeliverResponseAsync(endpointUrl);
        }

        /// <summary>
        /// Returns content items as JSON data. By default, retrieves one level of linked items.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<JObject> GetItemsJsonAsync(params string[] parameters)
        {
            var endpointUrl = UrlBuilder.GetItemsUrl(parameters);

            return await GetDeliverResponseAsync(endpointUrl);
        }

        /// <summary>
        /// Returns a content item. By default, retrieves one level of linked items.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for projection or setting the depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemResponse"/> instance that contains the content item with the specified codename.</returns>
        public async Task<DeliveryItemResponse> GetItemAsync(string codename, params IQueryParameter[] parameters)
        {
            return await GetItemAsync(codename, (IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Gets a strongly typed content item by its codename. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for projection or setting the depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemResponse{T}"/> instance that contains the content item with the specified codename.</returns>
        public async Task<DeliveryItemResponse<T>> GetItemAsync<T>(string codename, params IQueryParameter[] parameters)
        {
            return await GetItemAsync<T>(codename, (IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns a content item. By default, retrieves one level of linked items.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example, for projection or setting the depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemResponse"/> instance that contains the content item with the specified codename.</returns>
        public async Task<DeliveryItemResponse> GetItemAsync(string codename, IEnumerable<IQueryParameter> parameters)
        {
            if (codename == null)
            {
                throw new ArgumentNullException(nameof(codename), "The codename of a content item is not specified.");
            }

            if (codename == string.Empty)
            {
                throw new ArgumentException("The codename of a content item is not specified.", nameof(codename));
            }

            var endpointUrl = UrlBuilder.GetItemUrl(codename, parameters);
            var response = await GetDeliverResponseAsync(endpointUrl);

            return new DeliveryItemResponse(response, ModelProvider, ContentLinkUrlResolver, endpointUrl);
        }

        /// <summary>
        /// Gets a strongly typed content item by its codename. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example, for projection or setting the depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemResponse{T}"/> instance that contains the content item with the specified codename.</returns>
        public async Task<DeliveryItemResponse<T>> GetItemAsync<T>(string codename, IEnumerable<IQueryParameter> parameters = null)
        {
            if (string.IsNullOrEmpty(codename))
            {
                throw new ArgumentException("Entered item codename is not valid.", nameof(codename));
            }

            var endpointUrl = UrlBuilder.GetItemUrl(codename, parameters);
            var response = await GetDeliverResponseAsync(endpointUrl);

            return new DeliveryItemResponse<T>(response, ModelProvider, endpointUrl);
        }

        /// <summary>
        /// Returns content items that match the optional filtering parameters. By default, retrieves one level of linked items.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<DeliveryItemListingResponse> GetItemsAsync(params IQueryParameter[] parameters)
        {
            return await GetItemsAsync((IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns content items that match the optional filtering parameters. By default, retrieves one level of linked items.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<DeliveryItemListingResponse> GetItemsAsync(IEnumerable<IQueryParameter> parameters)
        {
            var endpointUrl = UrlBuilder.GetItemsUrl(parameters);
            var response = await GetDeliverResponseAsync(endpointUrl);

            return new DeliveryItemListingResponse(response, ModelProvider, ContentLinkUrlResolver, endpointUrl);
        }

        /// <summary>
        /// Returns strongly typed content items that match the optional filtering parameters. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse{T}"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<DeliveryItemListingResponse<T>> GetItemsAsync<T>(params IQueryParameter[] parameters)
        {
            return await GetItemsAsync<T>((IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns strongly typed content items that match the optional filtering parameters. By default, retrieves one level of linked items.
        /// </summary>
        /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">A collection of query parameters, for example, for filtering, ordering, or setting the depth of linked items.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse{T}"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<DeliveryItemListingResponse<T>> GetItemsAsync<T>(IEnumerable<IQueryParameter> parameters)
        {
            var enhancedParameters = ExtractParameters<T>(parameters);
            var endpointUrl = UrlBuilder.GetItemsUrl(enhancedParameters);
            var response = await GetDeliverResponseAsync(endpointUrl);

            return new DeliveryItemListingResponse<T>(response, ModelProvider, endpointUrl);
        }

        /// <summary>
        /// Returns a content type as JSON data.
        /// </summary>
        /// <param name="codename">The codename of a content type.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content type with the specified codename.</returns>
        public async Task<JObject> GetTypeJsonAsync(string codename)
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

            return await GetDeliverResponseAsync(endpointUrl);
        }

        /// <summary>
        /// Returns content types as JSON data.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for paging.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        public async Task<JObject> GetTypesJsonAsync(params string[] parameters)
        {
            var endpointUrl = UrlBuilder.GetTypesUrl(parameters);

            return await GetDeliverResponseAsync(endpointUrl);
        }

        /// <summary>
        /// Returns a content type.
        /// </summary>
        /// <param name="codename">The codename of a content type.</param>
        /// <returns>The content type with the specified codename.</returns>
        public async Task<ContentType> GetTypeAsync(string codename)
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
            var response = await GetDeliverResponseAsync(endpointUrl);

            return new ContentType(response);
        }

        /// <summary>
        /// Returns content types.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for paging.</param>
        /// <returns>The <see cref="DeliveryTypeListingResponse"/> instance that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        public async Task<DeliveryTypeListingResponse> GetTypesAsync(params IQueryParameter[] parameters)
        {
            return await GetTypesAsync((IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns content types.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for paging.</param>
        /// <returns>The <see cref="DeliveryTypeListingResponse"/> instance that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        public async Task<DeliveryTypeListingResponse> GetTypesAsync(IEnumerable<IQueryParameter> parameters)
        {
            var endpointUrl = UrlBuilder.GetTypesUrl(parameters);
            var response = await GetDeliverResponseAsync(endpointUrl);

            return new DeliveryTypeListingResponse(response, endpointUrl);
        }

        /// <summary>
        /// Returns a content element.
        /// </summary>
        /// <param name="contentTypeCodename">The codename of the content type.</param>
        /// <param name="contentElementCodename">The codename of the content element.</param>
        /// <returns>A content element with the specified codename that is a part of a content type with the specified codename.</returns>
        public async Task<ContentElement> GetContentElementAsync(string contentTypeCodename, string contentElementCodename)
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
            var response = await GetDeliverResponseAsync(endpointUrl);

            var elementCodename = response["codename"].ToString();

            return new ContentElement(response, elementCodename);
        }


        /// <summary>
        /// Returns a taxonomy group as JSON data.
        /// </summary>
        /// <param name="codename">The codename of a taxonomy group.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the taxonomy group with the specified codename.</returns>
        public async Task<JObject> GetTaxonomyJsonAsync(string codename)
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

            return await GetDeliverResponseAsync(endpointUrl);
        }

        /// <summary>
        /// Returns taxonomy groups as JSON data.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for paging.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the taxonomy groups. If no query parameters are specified, all taxonomy groups are returned.</returns>
        public async Task<JObject> GetTaxonomiesJsonAsync(params string[] parameters)
        {
            var endpointUrl = UrlBuilder.GetTaxonomiesUrl(parameters);

            return await GetDeliverResponseAsync(endpointUrl);
        }

        /// <summary>
        /// Returns a taxonomy group.
        /// </summary>
        /// <param name="codename">The codename of a taxonomy group.</param>
        /// <returns>The taxonomy group with the specified codename.</returns>
        public async Task<TaxonomyGroup> GetTaxonomyAsync(string codename)
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
            var response = await GetDeliverResponseAsync(endpointUrl);

            return new TaxonomyGroup(response);
        }

        /// <summary>
        /// Returns taxonomy groups.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for paging.</param>
        /// <returns>The <see cref="DeliveryTaxonomyListingResponse"/> instance that represents the taxonomy groups. If no query parameters are specified, all taxonomy groups are returned.</returns>
        public async Task<DeliveryTaxonomyListingResponse> GetTaxonomiesAsync(params IQueryParameter[] parameters)
        {
            return await GetTaxonomiesAsync((IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns taxonomy groups.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for paging.</param>
        /// <returns>The <see cref="DeliveryTaxonomyListingResponse"/> instance that represents the taxonomy groups. If no query parameters are specified, all taxonomy groups are returned.</returns>
        public async Task<DeliveryTaxonomyListingResponse> GetTaxonomiesAsync(IEnumerable<IQueryParameter> parameters)
        {
            var endpointUrl = UrlBuilder.GetTaxonomiesUrl(parameters);
            var response = await GetDeliverResponseAsync(endpointUrl);

            return new DeliveryTaxonomyListingResponse(response, endpointUrl);
        }

        private async Task<JObject> GetDeliverResponseAsync(string endpointUrl)
        {
            if (DeliveryOptions.UsePreviewApi && DeliveryOptions.UseSecuredProductionApi)
            {
                throw new InvalidOperationException("Preview API and secured Delivery API must not be configured at the same time.");
            }

            if (DeliveryOptions.EnableResilienceLogic)
            {
                // Use the resilience logic.
                var policyResult = await ResiliencePolicyProvider?.Policy?.ExecuteAndCaptureAsync(() =>
                    {
                        return SendHttpMessage(endpointUrl);
                    }
                );

                return await GetResponseContent(policyResult?.FinalHandledResult ?? policyResult?.Result);
            }

            // Omit using the resilience logic completely.
            return await GetResponseContent(await SendHttpMessage(endpointUrl));
        }

        private Task<HttpResponseMessage> SendHttpMessage(string endpointUrl)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, endpointUrl);

            message.Headers.AddSdkTrackingHeader();

            if (DeliveryOptions.WaitForLoadingNewContent)
            {
                message.Headers.AddWaitForLoadingNewContentHeader();
            }

            if (UseSecuredProductionApi())
            {
                message.Headers.AddAuthorizationHeader("Bearer", DeliveryOptions.SecuredProductionApiKey);
            }

            if (UsePreviewApi())
            {
                message.Headers.AddAuthorizationHeader("Bearer", DeliveryOptions.PreviewApiKey);
            }

            return HttpClient.SendAsync(message);
        }

        private bool UseSecuredProductionApi()
        {
            return DeliveryOptions.UseSecuredProductionApi && !string.IsNullOrEmpty(DeliveryOptions.SecuredProductionApiKey);
        }

        private bool UsePreviewApi()
        {
            return DeliveryOptions.UsePreviewApi && !string.IsNullOrEmpty(DeliveryOptions.PreviewApiKey);
        }

        private async Task<JObject> GetResponseContent(HttpResponseMessage httpResponseMessage)
        {
            if (httpResponseMessage?.StatusCode == HttpStatusCode.OK)
            {
                var content = await httpResponseMessage.Content?.ReadAsStringAsync();

                return JObject.Parse(content);
            }

            string faultContent = null;

            // The null-coallescing operator causes tests to fail for NREs, hence the "if" statement.
            if (httpResponseMessage?.Content != null)
            {
                faultContent = await httpResponseMessage.Content.ReadAsStringAsync();
            }

            throw new DeliveryException(httpResponseMessage, "Either the retry policy was disabled or all retry attempts were depleted.\nFault content:\n" + faultContent);
        }

        internal IEnumerable<IQueryParameter> ExtractParameters<T>(IEnumerable<IQueryParameter> parameters = null)
        {
            var enhancedParameters = parameters != null
                ? new List<IQueryParameter>(parameters)
                : new List<IQueryParameter>();

            var codename = TypeProvider.GetCodename(typeof(T));

            if (codename != null && !IsTypeInQueryParameters(parameters))
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
    }
}
