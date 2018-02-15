using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KenticoCloud.Delivery.QueryParameters.Utilities
{
    internal class ContentTypeExtractor
    {
        internal IEnumerable<IQueryParameter> ExtractParameters<T>(IEnumerable<IQueryParameter> parameters = null)
        {
            var contentTypeCodenameRetrieved = TryGetContentTypeCodename(typeof(T), out string contentTypeCodename);
            var enhancedParameters = parameters != null ? new List<IQueryParameter>(parameters) : new List<IQueryParameter>();

            if (contentTypeCodenameRetrieved && (parameters == null || !parameters.Any(p => p.GetType() == typeof(EqualsFilter) && (p as EqualsFilter).ElementOrAttributePath.Equals("system.type", StringComparison.Ordinal))))
            {
                enhancedParameters.Add(new EqualsFilter("system.type", contentTypeCodename));
            }
            return enhancedParameters;
        }

        internal bool TryGetContentTypeCodename(Type contentType, out string codename)
        {
            var fields = contentType.GetFields(BindingFlags.Static | BindingFlags.Public);
            string codenameField = "Codename";

            if (fields.Any(field => field.Name == codenameField))
            {
                codename = (string)contentType.GetField(codenameField)?.GetValue(null);
                return codename != null;
            }

            codename = null;

            return false;
        }
    }
}
