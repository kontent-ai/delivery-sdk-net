using System;
using System.Collections.Generic;

#if (DEBUG && NET45)
using System.Configuration;
#endif

using System.Linq;

namespace KenticoCloud.Delivery
{
	internal class DeliveryEndpointUrlBuilder
	{
#if (DEBUG && NET45)
        private string PRODUCTION_ENDPOINT = ConfigurationManager.AppSettings["ProductionEndpoint"] ?? "https://deliver.kenticocloud.com/{0}";
        private string PREVIEW_ENDPOINT = ConfigurationManager.AppSettings["PreviewEndpoint"] ?? "https://preview-deliver.kenticocloud.com/{0}";
#else
        private const string PRODUCTION_ENDPOINT = "https://deliver.kenticocloud.com/{0}";
        private const string PREVIEW_ENDPOINT = "https://preview-deliver.kenticocloud.com/{0}";
#endif

        private const string URL_TEMPLATE_ITEMS = "/items/{0}";
        private const string URL_TEMPLATE_TYPES = "/types/{0}";
        private const string URL_TEMPLATE_TYPE_ELEMENT = "/types/{0}/elements/{1}";

        private readonly string projectId;
        private readonly string accessToken;

        public DeliveryEndpointUrlBuilder(string projectId)
        {
            this.projectId = projectId;
        }

        public DeliveryEndpointUrlBuilder(string projectId, string accessToken)
        {
            this.projectId = projectId;
            this.accessToken = accessToken;
        }

        public string GetItemsUrl(string itemCodename = "", params string[] queryParams)
        {
            var baseUrl = GetBaseUrl();
            var urlPath = String.Format(URL_TEMPLATE_ITEMS, Uri.EscapeDataString(itemCodename));

            return baseUrl + urlPath + "?" + String.Join("&", queryParams);
        }

        public string GetItemsUrl(string itemCodename = "", IEnumerable<IFilter> filters = null)
        {
            var url = GetBaseUrl();
            url += String.Format(URL_TEMPLATE_ITEMS, Uri.EscapeDataString(itemCodename));

            if (filters != null && filters.Any())
            {
                url += "?" + String.Join("&", filters.Select(f => f.GetQueryStringParameter()));
            }

            return url;
        }

        public string GetTypesUrl(string typeCodename = "", params string[] queryParams)
        {
            var baseUrl = GetBaseUrl();
            var urlPath = String.Format(URL_TEMPLATE_TYPES, Uri.EscapeDataString(typeCodename));

            return baseUrl + urlPath + "?" + String.Join("&", queryParams);
        }

        public string GetTypesUrl(string typeCodename = "", IEnumerable<IFilter> filters = null)
        {
            var url = GetBaseUrl();
            url += String.Format(URL_TEMPLATE_TYPES, Uri.EscapeDataString(typeCodename));

			if (filters != null && filters.Any())
			{
				url += "?" + String.Join("&", filters.Select(f => f.GetQueryStringParameter()));
			}

			return url;
		}

        public string GetTypeElementUrl(string typeCodename, string elementCodename)
        {
            var baseUrl = GetBaseUrl();
            var urlPath = String.Format(URL_TEMPLATE_TYPE_ELEMENT, Uri.EscapeDataString(typeCodename), Uri.EscapeDataString(elementCodename));

            return baseUrl + urlPath;
        }

        private string GetBaseUrl()
        {
            var url = String.IsNullOrEmpty(accessToken) ? PRODUCTION_ENDPOINT : PREVIEW_ENDPOINT;

            return String.Format(url, projectId);
        }
    }
}