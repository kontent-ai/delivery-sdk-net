using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using KenticoCloud.Delivery.ContentItemsInRichText;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Executes requests against the Kentico Cloud Delivery API.
    /// </summary>
    public sealed class DeliveryClient : IDeliveryClient
    {
        private const int PROJECT_ID_MAX_LENGTH = 36;

        private readonly HttpClient _httpClient;
        private readonly DeliveryEndpointUrlBuilder _urlBuilder;

        private IContentLinkUrlResolver _linkUrlResolver;

        /// <summary>
        /// Richtext content items processor for content items retrieved using this client.
        /// </summary>
        public InlineContentItemsProcessor InlineContentItemsProcessor { get; private set; }

        private ContentLinkResolver _linkResolver;
        private ICodeFirstModelProvider _codeFirstModelProvider;

        /// <summary>
        /// Gets or sets an object that resolves links to content items in Rich text element values.
        /// </summary>
        public IContentLinkUrlResolver ContentLinkUrlResolver
        {
            get
            {
                return _linkUrlResolver;
            }
            set
            {
                _linkUrlResolver = value;
                _linkResolver = new ContentLinkResolver(_linkUrlResolver);
            }
        }

        /// <summary>
        /// Gets or sets an object that performs conversion of content items to code-first models.
        /// </summary>
        public ICodeFirstModelProvider CodeFirstModelProvider
        {
            get { return _codeFirstModelProvider ?? (_codeFirstModelProvider = new CodeFirstModelProvider(this)); }
            set { _codeFirstModelProvider = value; }
        }

        internal ContentLinkResolver ContentLinkResolver
        {
            get
            {
                return _linkResolver;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryClient"/> class for the published content of the specified project.
        /// </summary>
        /// <param name="projectId">The identifier of the Kentico Cloud project.</param>
        public DeliveryClient(string projectId)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId), "Kentico Cloud project identifier is not specified.");
            }

            if (projectId == string.Empty)
            {
                throw new ArgumentException("Kentico Cloud project identifier is not specified.", nameof(projectId));
            }

            if (projectId.Length > PROJECT_ID_MAX_LENGTH)
            {
                throw new ArgumentException($"The specified Kentico cloud project identifier ({projectId}) is too long. Haven't you accidentally passed the Preview API key instead of the project identifier?", nameof(projectId));
            }
            _urlBuilder = new DeliveryEndpointUrlBuilder(projectId);
            _httpClient = new HttpClient();
            var unretrievedInlineContentItemsResolver = new ReplaceWithEmptyStringForUnretrievedItemsResolver();
            var defaultInlineContentItemsResolver = new ReplaceWithEmptyStringResolver();
            InlineContentItemsProcessor = new InlineContentItemsProcessor(defaultInlineContentItemsResolver, unretrievedInlineContentItemsResolver);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryClient"/> class for the unpublished content of the specified project.
        /// </summary>
        /// <param name="projectId">The identifier of the Kentico Cloud project.</param>
        /// <param name="previewApiKey">The Preview API key.</param>
        public DeliveryClient(string projectId, string previewApiKey)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId), "Kentico Cloud project identifier is not specified.");
            }

            if (projectId == string.Empty)
            {
                throw new ArgumentException("Kentico Cloud project identifier is not specified.", nameof(projectId));
            }

            if (projectId.Length > PROJECT_ID_MAX_LENGTH)
            {
                throw new ArgumentException($"The specified Kentico cloud project identifier ({projectId}) is too long. Haven't you accidentally passed the Preview API key instead of the project identifier?", nameof(projectId));
            }

            if (previewApiKey == null)
            {
                throw new ArgumentNullException(nameof(projectId), "The Preview API key is not specified.");
            }

            if (previewApiKey == string.Empty)
            {
                throw new ArgumentException("The Preview API key is not specified.", nameof(projectId));
            }
            _urlBuilder = new DeliveryEndpointUrlBuilder(projectId, previewApiKey);
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", previewApiKey));
            var unretrievedInlineContentItemsResolver = new ReplaceWithWarningAboutUnretrievedItemResolver();
            var defaultInlineContentItemsResolver = new ReplaceWithWarningAboutRegistrationResolver();
            InlineContentItemsProcessor = new InlineContentItemsProcessor(defaultInlineContentItemsResolver, unretrievedInlineContentItemsResolver);
        }

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

            var endpointUrl = _urlBuilder.GetItemUrl(codename, parameters);

            return await GetDeliverResponseAsync(endpointUrl);
        }

        /// <summary>
        /// Returns content items as JSON data. By default, retrieves one level of modular content.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for filtering, ordering, or setting the depth of modular content.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<JObject> GetItemsJsonAsync(params string[] parameters)
        {
            var endpointUrl = _urlBuilder.GetItemsUrl(parameters);

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

            var endpointUrl = _urlBuilder.GetItemUrl(codename, parameters);
            var response = await GetDeliverResponseAsync(endpointUrl);

            return new DeliveryItemResponse(response, this);
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

            var url = _urlBuilder.GetItemUrl(codename, parameters);
            var response = await GetDeliverResponseAsync(url);

            return new DeliveryItemResponse<T>(response, this);
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
            var endpointUrl = _urlBuilder.GetItemsUrl(parameters);
            var response = await GetDeliverResponseAsync(endpointUrl);

            return new DeliveryItemListingResponse(response, this);
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
            var endpointUrl = _urlBuilder.GetItemsUrl(parameters);
            var response = await GetDeliverResponseAsync(endpointUrl);

            return new DeliveryItemListingResponse<T>(response, this);
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

            var endpointUrl = _urlBuilder.GetTypeUrl(codename);

            return await GetDeliverResponseAsync(endpointUrl);
        }

        /// <summary>
        /// Returns content types as JSON data.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example, for paging.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        public async Task<JObject> GetTypesJsonAsync(params string[] parameters)
        {
            var endpointUrl = _urlBuilder.GetTypesUrl(parameters);

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

            var endpointUrl = _urlBuilder.GetTypeUrl(codename);
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
            var endpointUrl = _urlBuilder.GetTypesUrl(parameters: parameters);
            var response = await GetDeliverResponseAsync(endpointUrl);

            return new DeliveryTypeListingResponse(response);
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

            var endpointUrl = _urlBuilder.GetContentElementUrl(contentTypeCodename, contentElementCodename);
            var response = await GetDeliverResponseAsync(endpointUrl);

            var elementType = response["type"].ToString();
            var elementCodename = response["codename"].ToString();

            return new ContentElement(response, elementCodename);
        }

        private async Task<JObject> GetDeliverResponseAsync(string endpointUrl)
        {
            var response = await _httpClient.GetAsync(endpointUrl);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();

                return JObject.Parse(content);
            }

            throw new DeliveryException(response.StatusCode, await response.Content.ReadAsStringAsync());
        }
    }
}
