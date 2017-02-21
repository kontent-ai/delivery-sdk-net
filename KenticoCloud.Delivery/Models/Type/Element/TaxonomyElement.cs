using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
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