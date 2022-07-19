using System;
using System.Collections.Generic;
using System.Linq;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Urls.Delivery.QueryParameters;
using Microsoft.Extensions.Options;

namespace Kontent.Ai.Urls.Delivery
{
    /// <summary>
    /// Facilitates the generation of valid URLs for Kontent Delivery API
    /// </summary>
    public class DeliveryEndpointUrlBuilder
    {
        private const int UrlMaxLength = 65519;
        private const string UrlTemplateItem = "/items/{0}";
        private const string UrlTemplateItems = "/items";
        private const string UrlTemplateItemsFeed = "/items-feed";
        private const string UrlTemplateType = "/types/{0}";
        private const string UrlTemplateTypes = "/types";
        private const string UrlTemplateElement = "/types/{0}/elements/{1}";
        private const string UrlTemplateTaxonomy = "/taxonomies/{0}";
        private const string UrlTemplateTaxonomies = "/taxonomies";
        private const string UrlTemplateLanguages = "/languages";

        private readonly IOptionsMonitor<DeliveryOptions> _deliveryOptionsMonitor;
        private DeliveryOptions deliveryOptions;

        private DeliveryOptions CurrentDeliveryOptions
        {
            get
            {
                return _deliveryOptionsMonitor?.CurrentValue ?? deliveryOptions;
            }
            set
            {
                deliveryOptions = value;
            }
        }

        /// <summary>
        /// Initializes the URL builder using <see cref="IOptionsMonitor{TOptions}"/>. Ideal for web-based scenarios and scenarios with dynamically changing configuration.
        /// </summary>
        /// <param name="deliveryOptions">The configuration wrapped in a notification object.</param>
        public DeliveryEndpointUrlBuilder(IOptionsMonitor<DeliveryOptions> deliveryOptions)
        {
            _deliveryOptionsMonitor = deliveryOptions;
        }

        /// <summary>
        /// Initializes the URL builder using <see cref="IOptionsMonitor{TOptions}"/>. Ideal for simple scenarios where hot reloading of configuration is not required.
        /// </summary>
        /// <param name="deliveryOptions">The configuration object.</param>
        [Obsolete("Use constructor DeliveryEndpointUrlBuilder(IOptionsMonitor<DeliveryOptions> deliveryOptions) for instance with DeliveryOptionsMonitor instead./> ")]
        public DeliveryEndpointUrlBuilder(DeliveryOptions deliveryOptions)
        {
            CurrentDeliveryOptions = deliveryOptions;
        }

        /// <summary>
        /// Generates an URL for retrieving a single content item.
        /// </summary>
        /// <param name="codename">ID of the item to be retrieved.</param>
        /// <param name="parameters">Additional filtering parameters.</param>
        /// <returns>A valid URL containing correctly formatted parameters.</returns>
        public string GetItemUrl(string codename, IEnumerable<IQueryParameter> parameters)
        {
            return GetUrl(string.Format(UrlTemplateItem, Uri.EscapeDataString(codename)), parameters);
        }

        /// <summary>
        /// Generates a URL for retrieving multiple content items.
        /// </summary>
        /// <param name="parameters">Filtering parameters.</param>
        /// <returns>A valid URL containing correctly formatted parameters.</returns>
        public string GetItemsUrl(IEnumerable<IQueryParameter> parameters)
        {
            var updatedParameters = EnrichParameters(parameters);
            return GetUrl(UrlTemplateItems, updatedParameters);
        }

        /// <summary>
        /// Generates an URL for enumerating all content items.
        /// </summary>
        /// <param name="parameters">Filtering parameters.</param>
        /// <returns>A valid URL containing correctly formatted parameters.</returns>
        public string GetItemsFeedUrl(IEnumerable<IQueryParameter> parameters)
        {
            return GetUrl(UrlTemplateItemsFeed, parameters);
        }

        /// <summary>
        /// Generates an URL for retrieving a single content type.
        /// </summary>
        /// <param name="codename">ID of the content type to be retrieved.</param>
        /// <param name="parameters">Additional filtering parameters.</param>
        /// <returns>A valid URL containing correctly formatted parameters.</returns>
        public string GetTypeUrl(string codename, IEnumerable<IQueryParameter> parameters = null)
        {
            return GetUrl(string.Format(UrlTemplateType, Uri.EscapeDataString(codename)), parameters);
        }

        /// <summary>
        /// Generates an URL for retrieving multiple content types.
        /// </summary>
        /// <param name="parameters">Filtering parameters.</param>
        /// <returns>A valid URL containing correctly formatted parameters.</returns>
        public string GetTypesUrl(IEnumerable<IQueryParameter> parameters)
        {
            return GetUrl(UrlTemplateTypes, parameters);
        }

        /// <summary>
        /// Generates an URL for retrieving a content element.
        /// </summary>
        /// <param name="contentTypeCodename">Codename of the parent content type.</param>
        /// <param name="contentElementCodename">Codename of the element.</param>
        /// <returns></returns>
        public string GetContentElementUrl(string contentTypeCodename, string contentElementCodename)
        {
            return GetUrl(string.Format(UrlTemplateElement, Uri.EscapeDataString(contentTypeCodename), Uri.EscapeDataString(contentElementCodename)));
        }

        /// <summary>
        /// Generates an URL for retrieving a single taxonomy.
        /// </summary>
        /// <param name="codename">ID of the content type to be retrieved.</param>
        /// <returns>A valid URL containing correctly formatted parameters.</returns>
        public string GetTaxonomyUrl(string codename)
        {
            return GetUrl(string.Format(UrlTemplateTaxonomy, Uri.EscapeDataString(codename)));
        }

        /// <summary>
        /// Generates an URL for retrieving multiple taxonomies.
        /// </summary>
        /// <param name="parameters">Filtering parameters.</param>
        /// <returns>A valid URL containing correctly formatted parameters.</returns>
        public string GetTaxonomiesUrl(IEnumerable<IQueryParameter> parameters)
        {
            return GetUrl(UrlTemplateTaxonomies, parameters);
        }


        /// <summary>
        /// Generates an URL for retrieving multiple languages.
        /// </summary>
        /// <param name="parameters">Filtering parameters.</param>
        /// <returns>A valid URL containing correctly formatted parameters.</returns>
        public string GetLanguagesUrl(IEnumerable<IQueryParameter> parameters)
        {
            return GetUrl(UrlTemplateLanguages, parameters);
        }

        private string GetUrl(string path, IEnumerable<IQueryParameter> parameters)
        {
            if (parameters != null)
            {
                var queryParameters = parameters.ToList();
                if (queryParameters.Any())
                {
                    return GetUrl(path, queryParameters.Select(parameter => parameter.GetQueryStringParameter()));
                }
            }

            return GetUrl(path);
        }

        private string GetUrl(string path, IEnumerable<string> parameters = null)
        {
            var hostUrl = AssembleHost();
            var url = AssembleUrl(path, parameters, hostUrl);

            if (url.Length > UrlMaxLength)
            {
                throw new UriFormatException("The request URL is too long. Split your query into multiple calls.");
            }

            return url;
        }

        private static string AssembleUrl(string path, IEnumerable<string> parameters, string hostUrl)
        {
            var urlBuilder = new UriBuilder(hostUrl + path);

            if (parameters != null)
            {
                urlBuilder.Query = string.Join("&", parameters);
            }

            return urlBuilder.ToString();
        }

        private string AssembleHost()
        {
            var endpointUrl = CurrentDeliveryOptions.UsePreviewApi
                            ? CurrentDeliveryOptions.PreviewEndpoint
                            : CurrentDeliveryOptions.ProductionEndpoint;
            var projectId = Uri.EscapeDataString(CurrentDeliveryOptions.ProjectId);
            var hostUrl = endpointUrl + projectId;
            return hostUrl;
        }

        private IEnumerable<IQueryParameter> EnrichParameters(IEnumerable<IQueryParameter> parameters)
        {
            var parameterList = parameters?.ToList() ?? new List<IQueryParameter>();
            if (CurrentDeliveryOptions.IncludeTotalCount && !parameterList.Any(x => x is IncludeTotalCountParameter))
            {
                parameterList.Add(new IncludeTotalCountParameter());
            }

            return parameterList;
        }
    }
}
