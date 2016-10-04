using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    public class Asset
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int Size { get; set; }
        public string Url { get; set; }


        public Asset(JToken asset)
        {
            Name = asset["name"].ToString();
            Type = asset["type"].ToString();
            Size = asset["size"].ToObject<int>();
            Url = asset["url"].ToString();
        }
    }
}
