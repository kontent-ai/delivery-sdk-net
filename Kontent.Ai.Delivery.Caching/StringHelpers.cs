using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Caching
{
    internal static class StringHelpers
    {
        internal static string Join(IEnumerable<string> strings)
        {
            return strings != null ? string.Join("|", strings) : string.Empty;
        }

        internal static string Join(params string[] strings)
        {
            return Join((IEnumerable<string>)strings);
        }
    }
}
