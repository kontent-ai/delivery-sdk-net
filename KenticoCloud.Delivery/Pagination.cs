using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents pagination information from Deliver response.
    /// </summary>
    public class Pagination
    {
        /// <summary>
        /// Number of items skipped.
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// Maximal number of items that might have been returned.
        /// </summary>
        public int Limit { get; set; }

        /// <summary>
        /// Number of items actually returned.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// URL to the next page.
        /// </summary>
        /// <remarks>Contains <see cref="string.Empty"/> if current response returned last page.</remarks>
        public string NextPageUrl { get; set; }

        /// <summary>
        /// Initializes pagination object.
        /// </summary>
        /// <param name="response">JSON returned from API.</param>
        public Pagination(JToken response)
        {
            Skip = response["skip"].ToObject<int>();
            Limit = response["limit"].ToObject<int>();
            Count = response["count"].ToObject<int>();
            NextPageUrl = response["next_page"].ToString();
        }
    }
}