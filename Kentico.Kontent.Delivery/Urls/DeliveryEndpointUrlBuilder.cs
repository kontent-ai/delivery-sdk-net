using System;
using System.Collections.Generic;
using System.Linq;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Urls.QueryParameters;
using Microsoft.Extensions.Options;

namespace Kentico.Kontent.Delivery.Urls
{
    internal sealed class DeliveryEndpointUrlBuilder
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

        private readonly IOptionsMonitor<DeliveryOptions> _deliveryOptions;

        public DeliveryEndpointUrlBuilder(IOptionsMonitor<DeliveryOptions> deliveryOptions)
        {
            _deliveryOptions = deliveryOptions;
        }

        public string GetItemUrl(string codename, string[] parameters)
        {
            return GetUrl(string.Format(UrlTemplateItem, Uri.EscapeDataString(codename)), parameters);
        }

        public string GetItemUrl(string codename, IEnumerable<IQueryParameter> parameters)
        {
            return GetUrl(string.Format(UrlTemplateItem, Uri.EscapeDataString(codename)), parameters);
        }

        public string GetItemsUrl(string[] parameters)
        {
            var enrichedParameters = EnrichParameters(parameters).ToArray();
            return GetUrl(UrlTemplateItems, enrichedParameters);
        }

        public string GetItemsUrl(IEnumerable<IQueryParameter> parameters)
        {
            var updatedParameters = EnrichParameters(parameters);
            return GetUrl(UrlTemplateItems, updatedParameters);
        }

        public string GetItemsFeedUrl(string[] parameters)
        {
            return GetUrl(UrlTemplateItemsFeed, parameters);
        }

        public string GetItemsFeedUrl(IEnumerable<IQueryParameter> parameters)
        {
            return GetUrl(UrlTemplateItemsFeed, parameters);
        }

        public string GetTypeUrl(string codename)
        {
            return GetUrl(string.Format(UrlTemplateType, Uri.EscapeDataString(codename)));
        }

        public string GetTypeUrl(string codename, IEnumerable<IQueryParameter> parameters)
        {
            return GetUrl(string.Format(UrlTemplateType, Uri.EscapeDataString(codename)), parameters);
        }

        public string GetTypesUrl(string[] parameters)
        {
            return GetUrl(UrlTemplateTypes, parameters);
        }

        public string GetTypesUrl(IEnumerable<IQueryParameter> parameters)
        {
            return GetUrl(UrlTemplateTypes, parameters);
        }

        public string GetContentElementUrl(string contentTypeCodename, string contentElementCodename)
        {
            return GetUrl(string.Format(UrlTemplateElement, Uri.EscapeDataString(contentTypeCodename), Uri.EscapeDataString(contentElementCodename)));
        }

        public string GetTaxonomyUrl(string codename)
        {
            return GetUrl(string.Format(UrlTemplateTaxonomy, Uri.EscapeDataString(codename)));
        }

        public string GetTaxonomiesUrl(string[] parameters)
        {
            return GetUrl(UrlTemplateTaxonomies, parameters);
        }

        public string GetTaxonomiesUrl(IEnumerable<IQueryParameter> parameters)
        {
            return GetUrl(UrlTemplateTaxonomies, parameters);
        }

        private string GetUrl(string path, IEnumerable<IQueryParameter> parameters)
        {
            if (parameters != null)
            {
                var queryParameters = parameters.ToList();
                if (queryParameters.Any())
                {
                    return GetUrl(path, queryParameters.Select(parameter => parameter.GetQueryStringParameter()).ToArray());
                }
            }

            return GetUrl(path);
        }

        private string GetUrl(string path, string[] parameters = null)
        {
            var hostUrl = AssembleHost();
            var url = AssembleUrl(path, parameters, hostUrl);

            if (url.Length > UrlMaxLength)
            {
                throw new UriFormatException("The request url is too long. Split your query into multiple calls.");
            }

            return url;
        }

        private static string AssembleUrl(string path, string[] parameters, string hostUrl)
        {
            var urlBuilder = new UriBuilder(hostUrl + path);

            if (parameters != null && parameters.Length > 0)
            {
                urlBuilder.Query = string.Join("&", parameters);
            }

            var url = urlBuilder.ToString();
            return url;
        }

        private string AssembleHost()
        {
            var endpointUrlTemplate = _deliveryOptions.CurrentValue.UsePreviewApi
                            ? _deliveryOptions.CurrentValue.PreviewEndpoint
                            : _deliveryOptions.CurrentValue.ProductionEndpoint;
            var projectId = Uri.EscapeDataString(_deliveryOptions.CurrentValue.ProjectId);
            var hostUrl = string.Format(endpointUrlTemplate, projectId);
            return hostUrl;
        }

        private IEnumerable<string> EnrichParameters(IEnumerable<string> parameters)
        {
            var parameterList = parameters?.ToList() ?? new List<string>();
            if (_deliveryOptions.CurrentValue.IncludeTotalCount && !parameterList.Contains(new IncludeTotalCountParameter().GetQueryStringParameter()))
            {
                parameterList.Add(new IncludeTotalCountParameter().GetQueryStringParameter());
            }

            return parameterList;
        }

        private IEnumerable<IQueryParameter> EnrichParameters(IEnumerable<IQueryParameter> parameters)
        {
            var parameterList = parameters?.ToList() ?? new List<IQueryParameter>();
            if (_deliveryOptions.CurrentValue.IncludeTotalCount && !parameterList.Any(x => x is IncludeTotalCountParameter))
            {
                parameterList.Add(new IncludeTotalCountParameter());
            }

            return parameterList;
        }
    }
}
