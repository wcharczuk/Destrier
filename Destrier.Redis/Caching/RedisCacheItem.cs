﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Destrier.Redis.Caching
{
    [RedisStore(RedisCache.Constants.RootKey)]
    public class RedisCacheItem
    {
        [RedisKey]
        public String Key { get; set; }

        public DateTime Created { get; set; }
        public DateTime? LastAccessed { get; set; }
        public DateTime? Expiration { get; set; }
        public long? SlidingExpirationMilliseconds { get; set; }

        [RedisBinarySerialize]
        public Object Value { get; set; }

        public long ValueSizeBytes { get; set; }
    }
}