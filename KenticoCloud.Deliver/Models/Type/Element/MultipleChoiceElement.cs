using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents the multiple choice element.
    /// </summary>
    public class MultipleChoiceElement: TypeElement, ITypeElement
    {
        /// <summary>
        /// List of all possible choices.
        /// </summary>
        public List<MultipleChoiceOption> Options { get; set; }

        /// <summary>
        /// Initializes multiple choice element.
        /// </summary>
        /// <param name="system">JSON with element's data.</param>
        public MultipleChoiceElement(JToken element, string codename = "")
            :base(element, codename)
        {
            Options = element["options"].ToObject<List<MultipleChoiceOption>>();
        }
    }
}
