using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents pagination information from Deliver response
    /// </summary>
    public class Pagination
    {
        /// <summary>
        /// How many content items were skipped.
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// Maximum number of items in a responce.
        /// </summary>
        public int Limit { get; set; }

        /// <summary>
        /// How many items the response contains.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// URL to the next page of items. If empty, there is no next page.
        /// </summary>
        public string NextPageUrl { get; set; }

        public Pagination(JToken response)
        {
            Skip = response["skip"].ToObject<int>();
            Limit = response["limit"].ToObject<int>();
            Count = response["count"].ToObject<int>();
            NextPageUrl = response["next_page"].ToString();
        }
    }
}
