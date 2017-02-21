using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represent a content type.
    /// </summary>
    public class ContentType
    {
        /// <summary>
        /// <see cref="Delivery.TypeSystem"/>
        /// </summary>
        public TypeSystem System { get; set; }

        /// <summary>
        /// Elements in its raw form.
        /// </summary>
        public Dictionary<string, ITypeElement> Elements { get; set; }

        /// <summary>
        /// Initializes content type from response JSON.
        /// </summary>
        /// <param name="item">JSON with type data.</param>
        public ContentType(JToken item)
        {
            if (item == null || !item.HasValues)
            {
                return;
            }

            System = new TypeSystem(item["system"]);
            Elements = new Dictionary<string, ITypeElement>();

            foreach (JProperty element in item["elements"])
            {
                var elementDefinition = element.First;
                var elementType = elementDefinition["type"].ToString();
                var elementCodename = element.Name;

                switch (elementType)
                {
                    case "multiple_choice":
                        Elements.Add(elementCodename, new MultipleChoiceElement(elementDefinition, elementCodename));
                        break;

                    case "taxonomy":
                        Elements.Add(elementCodename, new TaxonomyElement(elementDefinition, elementCodename));
                        break;

                    default:
                        Elements.Add(elementCodename, new TypeElement(elementDefinition, elementCodename));
                        break;
                }
            }
        }
    }
}