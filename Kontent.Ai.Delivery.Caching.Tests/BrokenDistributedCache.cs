﻿using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Caching.Tests
{
    public class BrokenDistributedCache : IDistributedCache
    {
        private readonly IMemoryCache _memCache;

        public BrokenDistributedCache(IOptions<MemoryDistributedCacheOptions> optionsAccessor)
            : this(optionsAccessor, NullLoggerFactory.Instance) { }

        public BrokenDistributedCache(IOptions<MemoryDistributedCacheOptions> optionsAccessor, ILoggerFactory loggerFactory)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _memCache = new MemoryCache(optionsAccessor.Value, loggerFactory);
        }

        public byte[] Get(string key)
        {
            throw new Exception("broken cache");
        }

        public Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return Task.FromResult(Get(key));
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var memoryCacheEntryOptions = new MemoryCacheEntryOptions();
            memoryCacheEntryOptions.AbsoluteExpiration = options.AbsoluteExpiration;
            memoryCacheEntryOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;
            memoryCacheEntryOptions.SlidingExpiration = options.SlidingExpiration;
            memoryCacheEntryOptions.Size = value.Length;

            _memCache.Set(key, value, memoryCacheEntryOptions);
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Set(key, value, options);
            return Task.CompletedTask;
        }

        public void Refresh(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _memCache.TryGetValue(key, out object value);
        }

        public Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Refresh(key);
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _memCache.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Remove(key);
            return Task.CompletedTask;
        }
    }
}
