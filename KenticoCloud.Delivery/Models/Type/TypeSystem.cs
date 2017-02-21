using Newtonsoft.Json.Linq;
using System;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents the system elements in a content type.
    /// </summary>
    public class TypeSystem
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
        /// Date and time when the content item was last modified.
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Initializes system information.
        /// </summary>
        /// <param name="system">JSON with system data.</param>
        public TypeSystem(JToken system)
        {
            Id = system["id"].ToString();
            Name = system["name"].ToString();
            Codename = system["codename"].ToString();
            LastModified = DateTime.Parse(system["last_modified"].ToString());
        }
    }
}