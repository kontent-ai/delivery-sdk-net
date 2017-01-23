using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents the multiple choice option.
    /// </summary>
    public class MultipleChoiceOption
    {
        /// <summary>
        /// Name of the choice.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Codename of the content item.
        /// </summary>
        public string Codename { get; set; }
    }
}
