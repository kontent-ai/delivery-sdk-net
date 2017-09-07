using System;
using System.Collections.Generic;
using System.Linq;

namespace KenticoCloud.Delivery
{
    internal sealed class DeliveryEndpointUrlBuilder
    {
        private const string URL_TEMPLATE_ITEM = "/items/{0}";
        private const string URL_TEMPLATE_ITEMS = "/items";
        private const string URL_TEMPLATE_TYPE = "/types/{0}";
        private const string URL_TEMPLATE_TYPES = "/types";
        private const string URL_TEMPLATE_ELEMENT = "/types/{0}/elements/{1}";
        private const string URL_TEMPLATE_TAXONOMY = "/taxonomies/{0}";
        private const string URL_TEMPLATE_TAXONOMIES = "/taxonomies";

        private readonly DeliveryOptions _deliveryOptions;

        public DeliveryEndpointUrlBuilder(DeliveryOptions deliveryOptions)
        {
            _deliveryOptions = deliveryOptions;
        }

        public string GetItemUrl(string codename, string[] parameters)
        {
            return GetUrl(string.Format(URL_TEMPLATE_ITEM, Uri.EscapeDataString(codename)), parameters);
        }

        public string GetItemUrl(string codename, IEnumerable<IQueryParameter> parameters)
        {
            return GetUrl(string.Format(URL_TEMPLATE_ITEM, Uri.EscapeDataString(codename)), parameters);
        }

        public string GetItemsUrl(string[] parameters)
        {
            return GetUrl(URL_TEMPLATE_ITEMS, parameters);
        }

        public string GetItemsUrl(IEnumerable<IQueryParameter> parameters)
        {
            return GetUrl(URL_TEMPLATE_ITEMS, parameters);
        }

        public string GetTypeUrl(string codename)
        {
            return GetUrl(string.Format(URL_TEMPLATE_TYPE, Uri.EscapeDataString(codename)));
        }

        public string GetTypeUrl(string codename, IEnumerable<IQueryParameter> parameters)
        {
            return GetUrl(string.Format(URL_TEMPLATE_TYPE, Uri.EscapeDataString(codename)), parameters);
        }

        public string GetTypesUrl(string[] parameters)
        {
            return GetUrl(URL_TEMPLATE_TYPES, parameters);
        }

        public string GetTypesUrl(IEnumerable<IQueryParameter> parameters)
        {
            return GetUrl(URL_TEMPLATE_TYPES, parameters);
        }

        public string GetContentElementUrl(string contentTypeCodename, string contentElementCodename)
        {
            return GetUrl(string.Format(URL_TEMPLATE_ELEMENT, Uri.EscapeDataString(contentTypeCodename), Uri.EscapeDataString(contentElementCodename)));
        }

        public string GetTaxonomyUrl(string codename)
        {
            return GetUrl(string.Format(URL_TEMPLATE_TAXONOMY, Uri.EscapeDataString(codename)));
        }

        public string GetTaxonomiesUrl(string[] parameters)
        {
            return GetUrl(URL_TEMPLATE_TAXONOMIES, parameters);
        }

        public string GetTaxonomiesUrl(IEnumerable<IQueryParameter> parameters)
        {
            return GetUrl(URL_TEMPLATE_TAXONOMIES, parameters);
        }

        private string GetUrl(string path, IEnumerable<IQueryParameter> parameters)
        {
            if (parameters != null && parameters.Any())
            {
                return GetUrl(path, parameters.Select(parameter => parameter.GetQueryStringParameter()).ToArray());
            }

            return GetUrl(path);
        }

        private string GetUrl(string path, string[] parameters = null)
        {
            var endpointUrl = string.Format(_deliveryOptions.UsePreviewApi ? _deliveryOptions.PreviewEndpoint : _deliveryOptions.ProductionEndpoint, Uri.EscapeDataString(_deliveryOptions.ProjectId));
            var baseUrl = string.Concat(endpointUrl, path);

            if (parameters != null && parameters.Length > 0)
            {
                return string.Concat(baseUrl, "?", string.Join("&", parameters));
            }

            return baseUrl;
        }
    }
}
