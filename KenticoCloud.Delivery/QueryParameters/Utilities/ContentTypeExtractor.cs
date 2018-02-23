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
            var enhancedParameters = parameters != null ? new List<IQueryParameter>(parameters) : new List<IQueryParameter>();
            
            if (!IsAlreadyInParameters(parameters) && TryGetContentTypeCodename(typeof(T), out string contentTypeCodename))
            {
                enhancedParameters.Add(new EqualsFilter("system.type", contentTypeCodename));
            }
            return enhancedParameters;
        }

        private bool IsAlreadyInParameters(IEnumerable<IQueryParameter> parameters)
        {
            var typeFilterExists = parameters?
                .OfType<EqualsFilter>()
                .Any(filter => filter
                    .ElementOrAttributePath
                    .Equals("system.type", StringComparison.Ordinal));
            return typeFilterExists ?? false;
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
