using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents the system elements in a content item.
    /// </summary>
    public class ItemSystem
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

        /// <summary>
        /// Initializes system information.
        /// </summary>
        /// <param name="system">JSON with system data.</param>
        public ItemSystem(JToken system)
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