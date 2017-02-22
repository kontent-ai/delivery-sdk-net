using System;
using System.Collections.Generic;
using System.Linq;

#if (DEBUG && NET45)
using System.Configuration;
#endif

namespace KenticoCloud.Delivery
{
    internal sealed class DeliveryEndpointUrlBuilder
    {
        #if (DEBUG && NET45)
        private string PRODUCTION_ENDPOINT = ConfigurationManager.AppSettings["ProductionEndpoint"] ?? "https://deliver.kenticocloud.com/{0}";
        private string PREVIEW_ENDPOINT = ConfigurationManager.AppSettings["PreviewEndpoint"] ?? "https://preview-deliver.kenticocloud.com/{0}";
        #else
        private const string PRODUCTION_ENDPOINT = "https://deliver.kenticocloud.com/{0}";
        private const string PREVIEW_ENDPOINT = "https://preview-deliver.kenticocloud.com/{0}";
        #endif

        private const string URL_TEMPLATE_ITEM = "/items/{0}";
        private const string URL_TEMPLATE_ITEMS = "/items";
        private const string URL_TEMPLATE_TYPE = "/types/{0}";
        private const string URL_TEMPLATE_TYPES = "/types";
        private const string URL_TEMPLATE_ELEMENT = "/types/{0}/elements/{1}";

        private readonly string projectId;
        private readonly string previewApiKey;

        public DeliveryEndpointUrlBuilder(string projectId)
        {
            this.projectId = projectId;
        }

        public DeliveryEndpointUrlBuilder(string projectId, string previewApiKey)
        {
            this.projectId = projectId;
            this.previewApiKey = previewApiKey;
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
            var endpointUrl = string.Format(string.IsNullOrEmpty(previewApiKey) ? PRODUCTION_ENDPOINT : PREVIEW_ENDPOINT, Uri.EscapeDataString(projectId));
            var baseUrl = string.Concat(endpointUrl, path);

            if (parameters != null && parameters.Length > 0)
            {
                return string.Concat(baseUrl, "?", string.Join("&", parameters));
            }

            return baseUrl;
        }
    }
}
