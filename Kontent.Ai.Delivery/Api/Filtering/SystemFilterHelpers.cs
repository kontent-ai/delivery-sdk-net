using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.Filtering;

internal static class SystemFilterHelpers
{
    internal static void AddSystemLanguageFilter(ICollection<KeyValuePair<string, string>> filters, string languageCodename)
    {
        ArgumentNullException.ThrowIfNull(filters);
        filters.Add(new KeyValuePair<string, string>(
            FilterPath.System("language") + FilterSuffix.Eq,
            FilterValueSerializer.Serialize(languageCodename)));
    }

    internal static void AddGenericTypeFilter<TModel>(
        ICollection<KeyValuePair<string, string>> filters,
        ITypeProvider typeProvider,
        ILogger? logger)
    {
        ArgumentNullException.ThrowIfNull(filters);
        ArgumentNullException.ThrowIfNull(typeProvider);

        if (ModelTypeHelper.IsDynamic<TModel>())
            return;

        var codename = typeProvider.GetCodename(typeof(TModel));

        if (string.IsNullOrEmpty(codename))
        {
            if (logger is not null)
            {
                LoggerMessages.GenericQueryTypeCodenameNotFound(logger, typeof(TModel).Name);
            }

            return;
        }

        filters.Add(new KeyValuePair<string, string>(
            FilterPath.System("type") + FilterSuffix.Eq,
            FilterValueSerializer.Serialize(codename)));
    }
}
