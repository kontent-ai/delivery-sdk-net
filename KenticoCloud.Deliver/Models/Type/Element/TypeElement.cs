using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace KenticoCloud.Deliver
{
    public class TypeElement : ITypeElement
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Codename { get; set; }

        /// <summary>
        /// Initializes type element information.
        /// </summary>
        /// <param name="element">JSON with element data.</param>
        public TypeElement(JToken element, string codename = "")
        {
            Type = element["type"].ToString();
            Name = element["name"].ToString();
            Codename = String.IsNullOrEmpty(codename) ? element["codename"].ToString() : codename;
        }
    }
}
