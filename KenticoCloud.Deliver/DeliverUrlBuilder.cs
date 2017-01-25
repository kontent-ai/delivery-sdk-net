using System;
using System.Collections.Generic;
#if (DEBUG && NET45)
using System.Configuration;
#endif

using System.Linq;

namespace KenticoCloud.Deliver
{
	internal class DeliverUrlBuilder
	{
		private readonly string projectId;
		private readonly string accessToken;

#if (DEBUG && NET45)
		private string PRODUCTION_ENDPOINT = ConfigurationManager.AppSettings["ProductionEndpoint"] ?? "https://deliver.kenticocloud.com/{0}/items/";
		private string PREVIEW_ENDPOINT = ConfigurationManager.AppSettings["PreviewEndpoint"] ?? "https://preview-deliver.kenticocloud.com/{0}/items/";
#else
		private const string PRODUCTION_ENDPOINT = "https://deliver.kenticocloud.com/{0}/items/";
		private const string PREVIEW_ENDPOINT = "https://preview-deliver.kenticocloud.com/{0}/items/";
#endif

		public DeliverUrlBuilder(string projectId, string accessToken = null)
		{
			this.projectId = projectId;
			this.accessToken = accessToken;
		}


		public string GetEndpointUrl(string codename = "", params string[] queryParams)
		{
			return GetBaseUrl(codename) + "?" + String.Join("&", queryParams);
		}


		public string GetEndpointUrl(string codename = "", IEnumerable<IFilter> filters = null)
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
