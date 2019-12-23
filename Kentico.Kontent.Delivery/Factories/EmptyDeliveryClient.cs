using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kentico.Kontent.Delivery.Factories
{
    internal class EmptyDeliveryClient : IDeliveryClient
    {
        public Task<DeliveryElementResponse> GetContentElementAsync(string contentTypeCodename, string contentElementCodename)
        {
            throw new NotImplementedException();
        }

        public Task<DeliveryItemResponse> GetItemAsync(string codename, params IQueryParameter[] parameters)
        {
            throw new NotImplementedException();
        }

        public Task<DeliveryItemResponse<T>> GetItemAsync<T>(string codename, params IQueryParameter[] parameters)
        {
            throw new NotImplementedException();
        }

        public Task<DeliveryItemResponse> GetItemAsync(string codename, IEnumerable<IQueryParameter> parameters)
        {
            throw new NotImplementedException();
        }

        public Task<DeliveryItemResponse<T>> GetItemAsync<T>(string codename, IEnumerable<IQueryParameter> parameters = null)
        {
            throw new NotImplementedException();
        }

        public Task<JObject> GetItemJsonAsync(string codename, params string[] parameters)
        {
            throw new NotImplementedException();
        }

        public Task<DeliveryItemListingResponse> GetItemsAsync(params IQueryParameter[] parameters)
        {
            throw new NotImplementedException();
        }

        public Task<DeliveryItemListingResponse> GetItemsAsync(IEnumerable<IQueryParameter> parameters)
        {
            throw new NotImplementedException();
        }

        public Task<DeliveryItemListingResponse<T>> GetItemsAsync<T>(params IQueryParameter[] parameters)
        {
            throw new NotImplementedException();
        }

        public Task<DeliveryItemListingResponse<T>> GetItemsAsync<T>(IEnumerable<IQueryParameter> parameters)
        {
            throw new NotImplementedException();
        }

        public IDeliveryItemsFeed GetItemsFeed(params IQueryParameter[] parameters)
        {
            throw new NotImplementedException();
        }

        public IDeliveryItemsFeed GetItemsFeed(IEnumerable<IQueryParameter> parameters)
        {
            throw new NotImplementedException();
        }

        public IDeliveryItemsFeed<T> GetItemsFeed<T>(params IQueryParameter[] parameters)
        {
            throw new NotImplementedException();
        }

        public IDeliveryItemsFeed<T> GetItemsFeed<T>(IEnumerable<IQueryParameter> parameters)
        {
            throw new NotImplementedException();
        }

        public Task<JObject> GetItemsJsonAsync(params string[] parameters)
        {
            throw new NotImplementedException();
        }

        public Task<DeliveryTaxonomyListingResponse> GetTaxonomiesAsync(params IQueryParameter[] parameters)
        {
            throw new NotImplementedException();
        }

        public Task<DeliveryTaxonomyListingResponse> GetTaxonomiesAsync(IEnumerable<IQueryParameter> parameters)
        {
            throw new NotImplementedException();
        }

        public Task<JObject> GetTaxonomiesJsonAsync(params string[] parameters)
        {
            throw new NotImplementedException();
        }

        public Task<DeliveryTaxonomyResponse> GetTaxonomyAsync(string codename)
        {
            throw new NotImplementedException();
        }

        public Task<JObject> GetTaxonomyJsonAsync(string codename)
        {
            throw new NotImplementedException();
        }

        public Task<DeliveryTypeResponse> GetTypeAsync(string codename)
        {
            throw new NotImplementedException();
        }

        public Task<JObject> GetTypeJsonAsync(string codename)
        {
            throw new NotImplementedException();
        }

        public Task<DeliveryTypeListingResponse> GetTypesAsync(params IQueryParameter[] parameters)
        {
            throw new NotImplementedException();
        }

        public Task<DeliveryTypeListingResponse> GetTypesAsync(IEnumerable<IQueryParameter> parameters)
        {
            throw new NotImplementedException();
        }

        public Task<JObject> GetTypesJsonAsync(params string[] parameters)
        {
            throw new NotImplementedException();
        }
    }
}
