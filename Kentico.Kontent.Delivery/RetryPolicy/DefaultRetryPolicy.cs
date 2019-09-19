using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Kentico.Kontent.Delivery.RetryPolicy
{
    internal class DefaultRetryPolicy : IRetryPolicy
    {
        public DefaultRetryPolicy(RetryPolicyOptions options)
        {
        }

        public Task<HttpResponseMessage> ExecuteAsync(Func<Task<HttpResponseMessage>> sendRequest)
        {
            throw new NotImplementedException();
        }
    }
}