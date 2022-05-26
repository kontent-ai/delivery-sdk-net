using Kentico.Kontent.Delivery.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace DeliverySDKWithAutofac
{
    public class ProjectAProvider : ITypeProvider
    {
        public Type GetType(string contentType)
        {
            switch (contentType)
            {
                case "article":
                    return typeof(Article);
                case "writer":
                    return typeof(Writer);

                default:
                    return null;
            }
        }

        public string GetCodename(Type contentType)
        {
            if (contentType == typeof(Article))
            {
                return "article";
            }
            if (contentType == typeof(Writer))
            {
                return "writer";
            }

            return null;
        }
    }

    public class ProjectBProvider : ITypeProvider
    {
        public Type GetType(string contentType)
        {
            switch (contentType)
            {
                case "movie":
                    return typeof(Movie);
                case "actor":
                    return typeof(Actor);

                default:
                    return null;
            }
        }

        public string GetCodename(Type contentType)
        {
            if (contentType == typeof(Movie))
            {
                return "movie";
            }
            if (contentType == typeof(Actor))
            {
                return "actor";
            }

            return null;
        }
    }
}
