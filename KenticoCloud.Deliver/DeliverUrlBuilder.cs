using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    internal class DeliverUrlBuilder
    {
#if DEBUG
        private string PRODUCTION_ENDPOINT = System.Configuration.ConfigurationManager.AppSettings["ProductionEndpoint"] ?? "http://deliver.kenticocloud.com/{0}/items/";
        private string PREVIEW_ENDPOINT = System.Configuration.ConfigurationManager.AppSettings["PreviewEndpoint"] ?? "http://preview.deliver.kenticocloud.com/{0}/items/";
#else
        private const string PRODUCTION_ENDPOINT = "http://deliver.kenticocloud.com/{0}/items/";
        private const string PREVIEW_ENDPOINT = "http://preview.deliver.kenticocloud.com/{0}/items/";
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


        public string GetUrlEndpoint(string codename = "", params string[] queryParams)
        {
            return GetBaseUrl(codename) + "?" + String.Join("&", queryParams);
        }


        public string ComposeDeliverUrl(string codename = "", IEnumerable<IFilter> filters = null)
        {
            var url = GetBaseUrl(codename);

            if (filters != null && filters.Any())
            {
                url += "?" + String.Join("&", filters.Select(f => f.GetQueryStringParameter()));
            }

            return url;
        }


        private string GetBaseUrl(string codename)
        {
            var endpoint = String.IsNullOrEmpty(accessToken) ? PRODUCTION_ENDPOINT : PREVIEW_ENDPOINT;

            return String.Format(endpoint, projectId) + Uri.EscapeDataString(codename);
        }
    }
}
