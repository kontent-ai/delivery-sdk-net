using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents the taxonomy element
    /// </summary>
    public class TaxonomyElement : TypeElement, ITypeElement
    {
        /// <summary>
        /// Taxonomy group codename.
        /// </summary>
        public string TaxonomyGroup { get; set; }

        /// <summary>
        /// Initializes taxonomy element.
        /// </summary>
        /// <param name="system">JSON with element's data.</param>
        public TaxonomyElement(JToken element, string codename = "")
            : base(element, codename)
        {
            TaxonomyGroup = element["taxonomy_group"].ToString();
        }
    }
}
