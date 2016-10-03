using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliverSDK
{
    public class DeliverException : Exception
    {
        public int StatusCode { get; }

        public override string Message { get; }

        public DeliverException(int statusCode, string message)
        {
            var errorMessage = JObject.Parse(message);

            StatusCode = statusCode;
            Message = errorMessage["message"].ToString();
        }
    }
}
