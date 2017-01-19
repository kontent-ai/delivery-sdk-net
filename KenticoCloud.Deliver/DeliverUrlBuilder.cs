using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace KenticoCloud.Deliver
{
    internal class DeliverUrlBuilder
    {
#if DEBUG
        private string PRODUCTION_ENDPOINT = ConfigurationManager.AppSettings["ProductionEndpoint"] ?? "https://deliver.kenticocloud.com/{0}/{1}/";
        private string PREVIEW_ENDPOINT = ConfigurationManager.AppSettings["PreviewEndpoint"] ?? "https://preview-deliver.kenticocloud.com/{0}/{1}/";
#else
        private const string PRODUCTION_ENDPOINT = "https://deliver.kenticocloud.com/{0}/{1}/";
        private const string PREVIEW_ENDPOINT = "https://preview-deliver.kenticocloud.com/{0}/{1}/";
#endif


        private readonly string projectId;
        private readonly string accessToken;


        public DeliverUrlBuilder(string projectId)
        {
            this.projectId = projectId;
        }


        public DeliverUrlBuilder(string projectId, string accessToken)
        {
            this.projectId = projectId;
            this.accessToken = accessToken;
        }


        public string GetEndpointUrl(string endpoint, string codename = "", params string[] queryParams)
        {
            return GetBaseUrl(endpoint, codename) + "?" + String.Join("&", queryParams);
        }


        public string GetEndpointUrl(string endpoint, string codename = "", IEnumerable<IFilter> filters = null)
        {
            var url = GetBaseUrl(codename, endpoint);

            if (filters != null && filters.Any())
            {
                url += "?" + String.Join("&", filters.Select(f => f.GetQueryStringParameter()));
            }

            return url;
        }


        private string GetBaseUrl(string codename, string endpoint)
        {
            var url = String.IsNullOrEmpty(accessToken) ? PRODUCTION_ENDPOINT : PREVIEW_ENDPOINT;

            return String.Format(url, projectId, endpoint) + Uri.EscapeDataString(codename);
        }
    }
}
