using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents an error response from the API.
    /// </summary>
    public class DeliverException : Exception
    {
        /// <summary>
        /// HTTP status code.
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Detailed message from the API.
        /// </summary>
        public override string Message { get; }

        public DeliverException(int statusCode, string message)
        {
            var errorMessage = JObject.Parse(message);

            StatusCode = statusCode;
            Message = errorMessage["message"].ToString();
        }
    }
}
