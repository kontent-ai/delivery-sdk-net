using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

using KenticoCloud.Delivery.Extensions;
using KenticoCloud.Delivery.InlineContentItems;
using KenticoCloud.Delivery.ResiliencePolicy;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Executes requests against the Kentico Cloud Delivery API.
    /// </summary>
    public sealed class DeliveryClient : IDeliveryClient
    {
        private readonly DeliveryOptions _deliveryOptions;
        private readonly IContentLinkUrlResolver _contentLinkUrlResolver;
        private readonly IInlineContentItemsProcessor _inlineContentItemsProcessor;
        private readonly ICodeFirstModelProvider _codeFirstModelProvider;

        private HttpClient _httpClient;
        private DeliveryEndpointUrlBuilder _urlBuilder;
        private IResiliencePolicyProvider _resiliencePolicyProvider;

        /// <summary>
        /// Gets or sets an object that performs conversion of content items to code-first models.
        /// </summary>
        //public ICodeFirstModelProvider CodeFirstModelProvider
        //{
        //    get { return _codeFirstModelProvider ?? (_codeFirstModelProvider = new CodeFirstModelProvider(_contentLinkUrlResolver, _inlineContentItemsProcessor)); }

        //    set { _codeFirstModelProvider = value; }
        //}

        private DeliveryEndpointUrlBuilder UrlBuilder
        {
            get { return _urlBuilder ?? (_urlBuilder = new DeliveryEndpointUrlBuilder(_deliveryOptions)); }
        }

        /// <summary>
        /// An instance of <see cref="System.Net.Http.HttpClient"/> for sending requests to KC endpoints.
        /// </summary>
        public HttpClient HttpClient
        {
            get
            {
                if (_httpClient == null)
                {
                    _httpClient = new HttpClient();
                }
                return _httpClient;
            }
            set { _httpClient = value; }
        }

        /// <summary>
        /// Gets or sets the retry policy provider for HTTP requests.
        /// </summary>
        public IResiliencePolicyProvider ResiliencePolicyProvider
        {
            get { return _resiliencePolicyProvider ?? (_resiliencePolicyProvider = new DefaultResiliencePolicyProvider(_deliveryOptions.MaxRetryAttempts)); }
            set { _resiliencePolicyProvider = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryClient"/> class for retrieving content of the specified project.
        /// </summary>
        /// <param name="deliveryOptions">The settings of the Kentico Cloud project.</param>
        public DeliveryClient(DeliveryOptions deliveryOptions)
        {
            _deliveryOptions = deliveryOptions ?? throw new ArgumentNullException(nameof(deliveryOptions), "The Delivery options object is not specified.");

            if (_deliveryOptions.ProjectId == null)
            {
                throw new ArgumentNullException(nameof(_deliveryOptions.ProjectId), "Kentico Cloud project identifier is not specified.");
            }

            if (_deliveryOptions.ProjectId == string.Empty)
            {
                throw new ArgumentException("Kentico Cloud project identifier is not specified.", nameof(_deliveryOptions.ProjectId));
            }

            if (!Guid.TryParse(_deliveryOptions.ProjectId, out Guid projectIdGuid))
            {
                throw new ArgumentException("Provided string is not a valid project identifier ({ProjectId}). Haven't you accidentally passed the Preview API key instead of the project identifier?", nameof(_deliveryOptions.ProjectId));
            }

            if (_deliveryOptions.UsePreviewApi)
            {
                if (_deliveryOptions.PreviewApiKey == null)
                {
                    throw new ArgumentNullException(nameof(_deliveryOptions.PreviewApiKey), "The Preview API key is not specified.");
                }

                if (_deliveryOptions.PreviewApiKey == string.Empty)
                {
                    throw new ArgumentException("The Preview API key is not specified.", nameof(_deliveryOptions.PreviewApiKey));
                }
            }

            _deliveryOptions.ProjectId = projectIdGuid.ToString("D");

            var unretrievedInlineContentItemsResolver = new ReplaceWithWarningAboutUnretrievedItemResolver();
            var defaultInlineContentItemsResolver = new ReplaceWithWarningAboutRegistrationResolver();
            _inlineContentItemsProcessor = new InlineContentItemsProcessor(defaultInlineContentItemsResolver, unretrievedInlineContentItemsResolver);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryClient"/> class for retrieving content of the specified project.
        /// </summary>
        /// <param name="deliveryOptions">The settings of the Kentico Cloud project.</param>
        /// <param name="contentLinkUrlResolver">An instance of an object that can resolve links in rich text elements</param>
        /// <param name="contentItemsProcessor">An instance of an object that can resolve modular content in rich text elements</param>
        /// <param name="codeFirstModelProvider">An instance of an object that can JSON responses into strongly typed CLR objects</param>
        /// <param name="retryPolicyProvider">A provider of a resilience (retry) policy.</param>
        public DeliveryClient(IOptions<DeliveryOptions> deliveryOptions, IContentLinkUrlResolver contentLinkUrlResolver = null, IInlineContentItemsProcessor contentItemsProcessor = null, ICodeFirstModelProvider codeFirstModelProvider = null, IResiliencePolicyProvider retryPolicyProvider = null) 
            : this(deliveryOptions.Value)
        {
            _contentLinkUrlResolver = contentLinkUrlResolver;
            _codeFirstModelProvider = codeFirstModelProvider;
            if (_inlineContentItemsProcessor != null)
            {
                _inlineContentItemsProcessor = contentItemsProcessor;
            }
            ResiliencePolicyProvider = retryPolicyProvider;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryClient"/> class for the published content of the specified project.
        /// </summary>
        /// <param name="projectId">The identifier of the Kentico Cloud project.</param>
        public DeliveryClient(string projectId) 
            : this(new DeliveryOptions { ProjectId = projectId })
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryClient"/> class for the unpublished content of the specified project.
        /// </summary>
        /// <param name="projectId">The identifier of the Kentico Cloud project.</param>
        /// <param name="previewApiKey">The Preview API key.</param>
        public DeliveryClient(string projectId, string previewApiKey) 
            : this(new DeliveryOptions { ProjectId = projectId, PreviewApiKey = previewApiKey, UsePreviewApi = true })
        { }

        /// <summary>
        /// Returns a content item as JSON data. By default, retrieves one level of modular content.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for projection or setting the depth of modular content.</param>
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
        /// Returns content items as JSON data. By default, retrieves one level of modular content.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for filtering, ordering, or setting the depth of modular content.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<JObject> GetItemsJsonAsync(params string[] parameters)
        {
            var endpointUrl = UrlBuilder.GetItemsUrl(parameters);

            return await GetDeliverResponseAsync(endpointUrl);
        }

        /// <summary>
        /// Returns a content item. By default, retrieves one level of modular content.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for projection or setting the depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemResponse"/> instance that contains the content item with the specified codename.</returns>
        public async Task<DeliveryItemResponse> GetItemAsync(string codename, params IQueryParameter[] parameters)
        {
            return await GetItemAsync(codename, (IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Gets a strongly typed content item by its codename. By default, retrieves one level of modular content.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for projection or setting the depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemResponse{T}"/> instance that contains the content item with the specified codename.</returns>
        public async Task<DeliveryItemResponse<T>> GetItemAsync<T>(string codename, params IQueryParameter[] parameters)
        {
            return await GetItemAsync<T>(codename, (IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns a content item. By default, retrieves one level of modular content.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example, for projection or setting the depth of modular content.</param>
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

            return new DeliveryItemResponse(response, _codeFirstModelProvider, _contentLinkUrlResolver, endpointUrl);
        }

        /// <summary>
        /// Gets a strongly typed content item by its codename. By default, retrieves one level of modular content.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example, for projection or setting the depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemResponse{T}"/> instance that contains the content item with the specified codename.</returns>
        public async Task<DeliveryItemResponse<T>> GetItemAsync<T>(string codename, IEnumerable<IQueryParameter> parameters = null)
        {
            if (string.IsNullOrEmpty(codename))
            {
                throw new ArgumentException("Entered item codename is not valid.", nameof(codename));
            }

            var endpointUrl = UrlBuilder.GetItemUrl(codename, parameters);
            var response = await GetDeliverResponseAsync(endpointUrl);

            return new DeliveryItemResponse<T>(response, _codeFirstModelProvider, endpointUrl);
        }

        /// <summary>
        /// Returns content items that match the optional filtering parameters. By default, retrieves one level of modular content.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for filtering, ordering, or setting the depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<DeliveryItemListingResponse> GetItemsAsync(params IQueryParameter[] parameters)
        {
            return await GetItemsAsync((IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns content items that match the optional filtering parameters. By default, retrieves one level of modular content.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example, for filtering, ordering, or setting the depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<DeliveryItemListingResponse> GetItemsAsync(IEnumerable<IQueryParameter> parameters)
        {
            var endpointUrl = UrlBuilder.GetItemsUrl(parameters);
            var response = await GetDeliverResponseAsync(endpointUrl);

            return new DeliveryItemListingResponse(response, _codeFirstModelProvider, _contentLinkUrlResolver, endpointUrl);
        }

        /// <summary>
        /// Returns strongly typed content items that match the optional filtering parameters. By default, retrieves one level of modular content.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for filtering, ordering, or setting the depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse{T}"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<DeliveryItemListingResponse<T>> GetItemsAsync<T>(params IQueryParameter[] parameters)
        {
            return await GetItemsAsync<T>((IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns strongly typed content items that match the optional filtering parameters. By default, retrieves one level of modular content.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">A collection of query parameters, for example, for filtering, ordering, or setting the depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse{T}"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<DeliveryItemListingResponse<T>> GetItemsAsync<T>(IEnumerable<IQueryParameter> parameters)
        {
            var enhancedParameters = ExtractParameters<T>(parameters);
            var endpointUrl = UrlBuilder.GetItemsUrl(enhancedParameters);
            var response = await GetDeliverResponseAsync(endpointUrl);

            return new DeliveryItemListingResponse<T>(response, _codeFirstModelProvider, endpointUrl);
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
            if (_deliveryOptions.UsePreviewApi && _deliveryOptions.UseSecuredProductionApi)
            {
                throw new InvalidOperationException("Preview API and secured Delivery API must not be configured at the same time.");
            }

            if (_deliveryOptions.EnableResilienceLogic)
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

            if (_deliveryOptions.WaitForLoadingNewContent)
            {
                message.Headers.AddWaitForLoadingNewContentHeader();
            }

            if (UseSecuredProductionApi())
            {
                message.Headers.AddAuthorizationHeader("Bearer", _deliveryOptions.SecuredProductionApiKey);
            }

            if (UsePreviewApi())
            {
                message.Headers.AddAuthorizationHeader("Bearer", _deliveryOptions.PreviewApiKey);
            }

            return HttpClient.SendAsync(message);
        }

        private bool UseSecuredProductionApi()
        {
            return _deliveryOptions.UseSecuredProductionApi && !string.IsNullOrEmpty(_deliveryOptions.SecuredProductionApiKey);
        }

        private bool UsePreviewApi()
        {
            return _deliveryOptions.UsePreviewApi && !string.IsNullOrEmpty(_deliveryOptions.PreviewApiKey);
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

            throw new DeliveryException(httpResponseMessage, faultContent);
        }

        internal IEnumerable<IQueryParameter> ExtractParameters<T>(IEnumerable<IQueryParameter> parameters = null)
        {
            var enhancedParameters = parameters != null
                ? new List<IQueryParameter>(parameters)
                : new List<IQueryParameter>();

            var codename = _codeFirstModelProvider.TypeProvider.GetCodename(typeof(T));

            if (codename != null && !IsTypeInQueryParameters(parameters))
            {
                enhancedParameters.Add(new EqualsFilter("system.type", codename));
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
