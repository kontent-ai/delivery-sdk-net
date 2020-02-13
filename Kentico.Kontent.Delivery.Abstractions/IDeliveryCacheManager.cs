using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kentico.Kontent.Delivery.Abstractions
{
    public interface IDeliveryCacheManager
    {
        Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> valueFactory, Func<T, bool> shouldCache = null, Func<T, IEnumerable<string>> dependenciesFactory = null);
        Task<bool> TryGetAsync<T>(string key, out T value);
        Task InvalidateDependencyAsync(string key);
        Task ClearAsync();
    }
}
