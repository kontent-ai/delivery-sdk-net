using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Class for querying Deliver API.
    /// </summary>
    public class DeliverClient
    {
        private const int PROJECT_ID_MAX_LENGTH = 36;
        private readonly HttpClient httpClient;
        private readonly DeliverUrlBuilder urlBuilder;

        /// <summary>
        /// Constructor for production API.
        /// </summary>
        /// <param name="projectId">Project ID.</param>
        /// <remarks>Production API connects to your published content.</remarks>
        public DeliverClient(string projectId)
        {
            if (projectId.Length > PROJECT_ID_MAX_LENGTH)
            {
                throw new ArgumentException($"The project ID provided seems to be corrupted. Provided value: {projectId}", nameof(projectId));
            }

            urlBuilder = new DeliverUrlBuilder(projectId);
            httpClient = new HttpClient();
        }


        /// <summary>
        /// Constructor for preview API.
        /// </summary>
        /// <param name="projectId">Project ID.</param>
        /// <param name="accessToken">Preview access token.</param>
        /// <remarks>Preview API connects to your unpublished content.</remarks>
        public DeliverClient(string projectId, string accessToken)
        {
            if (projectId.Length > PROJECT_ID_MAX_LENGTH)
            {
                throw new ArgumentException($"The project ID provided seems to be corrupted. Provided value: {projectId}", nameof(projectId));
            }

            urlBuilder = new DeliverUrlBuilder(projectId, accessToken);
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", accessToken));
        }


        /// <summary>
        /// Gets one content item by its codename. This method returns the whole response as JObject.
        /// </summary>
        /// <param name="itemCodename">Content item codename.</param>
        /// <param name="queryParams">Query parameters. For example: "elements=title" or "depth=0"."</param>
        public async Task<JObject> GetItemJsonAsync(string itemCodename, params string[] queryParams)
        {
            if (String.IsNullOrEmpty(itemCodename))
            {
                throw new ArgumentException("Entered item codename is not valid.", nameof(itemCodename));
            }

            var url = urlBuilder.GetItemsUrl(itemCodename, queryParams);
            return await GetDeliverResponseAsync(url);
        }


        /// <summary>
        /// Searches the content repository for items that match the criteria. This method returns the whole response as JObject.
        /// </summary>
        /// <param name="queryParams">Query parameters. For example: "elements=title" or "depth=0".</param>
        public async Task<JObject> GetItemsJsonAsync(params string[] queryParams)
        {
            var url = urlBuilder.GetItemsUrl(queryParams: queryParams);
            return await GetDeliverResponseAsync(url);
        }


        /// <summary>
        /// Gets one content item by its codename.
        /// </summary>
        /// <param name="itemCodename">Content item codename.</param>
        /// <param name="parameters">Query parameters.</param>
        public async Task<DeliverItemResponse> GetItemAsync(string itemCodename, IEnumerable<IFilter> parameters = null)
        {
            if (String.IsNullOrEmpty(itemCodename))
            {
                throw new ArgumentException("Entered item codename is not valid.", nameof(itemCodename));
            }

            var url = urlBuilder.GetItemsUrl(itemCodename, parameters);
            var response = await GetDeliverResponseAsync(url);

            return new DeliverItemResponse(response);
        }


        /// <summary>
        /// Searches the content repository for items that match the filter criteria.
        /// </summary>
        /// <param name="parameters">Query parameters.</param>
        public async Task<DeliverItemListingResponse> GetItemsAsync(IEnumerable<IFilter> parameters = null)
        {
            var url = urlBuilder.GetItemsUrl(filters: parameters);
            var response = await GetDeliverResponseAsync(url);

            return new DeliverItemListingResponse(response);
        }


        /// <summary>
        /// Gets one content type by its codename. This method returns the whole response as JObject.
        /// </summary>
        /// <param name="itemCodename">Content type codename.</param>
        /// <param name="queryParams">Query parameters.</param>
        public async Task<JObject> GetTypeJsonAsync(string typeCodename, params string[] queryParams)
        {
            if (String.IsNullOrEmpty(typeCodename))
            {
                throw new ArgumentException("Entered type codename is not valid.", nameof(typeCodename));
            }

            var url = urlBuilder.GetTypesUrl(typeCodename, queryParams);
            return await GetDeliverResponseAsync(url);
        }


        /// <summary>
        /// Searches the content repository for types that match the criteria. This method returns the whole response as JObject.
        /// </summary>
        /// <param name="queryParams">Query parameters.</param>
        public async Task<JObject> GetTypesJsonAsync(params string[] queryParams)
        {
            var url = urlBuilder.GetTypesUrl(queryParams: queryParams);
            return await GetDeliverResponseAsync(url);
        }


        /// <summary>
        /// Gets one content type by its codename.
        /// </summary>
        /// <param name="itemCodename">Content type codename.</param>
        /// <param name="parameters">Query parameters.</param>
        public async Task<ContentType> GetTypeAsync(string typeCodename, IEnumerable<IFilter> parameters = null)
        {
            if (String.IsNullOrEmpty(typeCodename))
            {
                throw new ArgumentException("Entered type codename is not valid.", nameof(typeCodename));
            }

            var url = urlBuilder.GetTypesUrl(typeCodename, parameters);
            var response = await GetDeliverResponseAsync(url);

            return new ContentType(response);
        }


        /// <summary>
        /// Searches the content repository for types that match the filter criteria.
        /// </summary>
        /// <param name="parameters">Query parameters.</param>
        public async Task<DeliverTypeListingResponse> GetTypesAsync(IEnumerable<IFilter> parameters = null)
        {
            var url = urlBuilder.GetTypesUrl(filters: parameters);
            var response = await GetDeliverResponseAsync(url);

            return new DeliverTypeListingResponse(response);
        }


        /// <summary>
        /// Gets the type element definition.
        /// </summary>
        /// <param name="typeCodename">Content type codename.</param>
        /// <param name="elementCodename">Content type element codename.</param>
        public async Task<ITypeElement> GetTypeElementAsync(string typeCodename, string elementCodename)
        {
            var url = urlBuilder.GetTypeElementUrl(typeCodename, elementCodename);
            var response = await GetDeliverResponseAsync(url);

            var elementType = response["type"].ToString();

            switch (elementType)
            {
                case "multiple_choice":
                    return new MultipleChoiceElement(response);

                case "taxonomy":
                    return new TaxonomyElement(response);

                default:
                    return new TypeElement(response);
            }
        }


        private async Task<JObject> GetDeliverResponseAsync(string url)
        {
            var response = await httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();

                return JObject.Parse(responseBody);
            }

            throw new DeliverException(response.StatusCode, await response.Content.ReadAsStringAsync());
        }
    }
}
