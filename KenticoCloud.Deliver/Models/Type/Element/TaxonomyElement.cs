using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace KenticoCloud.Deliver
{
    public class TaxonomyElement : TypeElement, ITypeElement
    {
        /// <summary>
        /// Taxonomy group codename.
        /// </summary>
        public string TaxonomyGroup { get; set; }

        public TaxonomyElement(JToken element)
            : base(element)
        {
            TaxonomyGroup = element["taxonomy_group"].ToString();
        }
    }
}
