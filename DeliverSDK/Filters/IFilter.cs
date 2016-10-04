using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    public interface IFilter
    {
        string Element { get; }
        string Value { get; }
        string Operator { get; }

        string GetQueryStringParameter();
    }
}
