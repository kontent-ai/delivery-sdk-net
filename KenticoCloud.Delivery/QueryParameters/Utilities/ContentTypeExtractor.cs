using System;
using System.Collections.Generic;
using System.Reflection;

namespace KenticoCloud.Delivery.QueryParameters.Utilities
{
    internal class ContentTypeExtractor
    {
        internal IEnumerable<IQueryParameter> ExtractParameters<T>(IEnumerable<IQueryParameter> parameters = null)
        {
            var contentTypeCodenameRetrieved = TryGetContentTypeCodename(typeof(T), out string contentTypeCodename);
            var enhancedParameters = parameters != null ? new List<IQueryParameter>(parameters) : new List<IQueryParameter>();

            if (contentTypeCodenameRetrieved)
            {
                enhancedParameters.Add(new EqualsFilter("system.type", contentTypeCodename));
            }
            return enhancedParameters;
        }

        internal bool TryGetContentTypeCodename(Type contentType, out string codename)
        {
            var fields = contentType.GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach(var field in fields)
            {
                if(field.Name == "Codename")
                {
                    codename = (string)contentType.GetField("Codename")?.GetValue(null);
                    return codename != null;
                }
            }
            codename = String.Empty;
            return false;
        }

    }
}
