using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents the system elements in a content item.
    /// </summary>
    public class SystemElements
    {
        /// <summary>
        /// Unique content item ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name of the content item.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Codename of the content item.
        /// </summary>
        public string Codename { get; set; }

        /// <summary>
        /// Item's content type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// List of sitemap location codenames.
        /// </summary>
        public List<string> SitemapLocations { get; set; }

        /// <summary>
        /// Date and time when the content item was last modified.
        /// </summary>
        public DateTime LastModified { get; set; }
        
        public SystemElements(JToken system)
        {
            Id = system["id"].ToString();
            Name = system["name"].ToString();
            Codename = system["codename"].ToString();
            Type = system["type"].ToString();
            SitemapLocations = ((JArray)system["sitemap_locations"]).ToObject<List<string>>();
            LastModified = DateTime.Parse(system["last_modified"].ToString());
        }
    }
}
