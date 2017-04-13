using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    public sealed class DeliveryItemResponse<T>
    {
        private readonly JToken _response;
        private readonly DeliveryClient _client;
        private dynamic _modularContent;
        private T _item;

        public T Item
        {
            get {
                if (_item == null)
                {
                    _item = _client.CodeFirstModelProvider.GetContentItemModel<T>(_response["item"], _response["modular_content"]);
                }
                return _item;
            }
        }

        public dynamic ModularContent
        {
            get { return _modularContent ?? (_modularContent = JObject.Parse(_response["modular_content"].ToString())); }
        }

        internal DeliveryItemResponse(JToken response, DeliveryClient client)
        {
            _response = response;
            _client = client;
        }
    }
}
