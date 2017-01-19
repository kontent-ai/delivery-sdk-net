using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace KenticoCloud.Deliver
{
    public class MultipleChoiceElement: TypeElement, ITypeElement
    {
        public List<MultipleChoiceOption> Options { get; set; }

        public MultipleChoiceElement(JToken element, string codename = "")
            :base(element, codename)
        {
            Options = element["options"].ToObject<List<MultipleChoiceOption>>();
        }
    }
}
