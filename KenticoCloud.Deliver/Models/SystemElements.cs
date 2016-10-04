using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    public class SystemElements
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Codename { get; set; }
        public string Type { get; set; }
        public List<string> SitemapLocations { get; set; }
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
