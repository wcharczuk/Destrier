using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Destrier.Redis.Cache
{
    [RedisStore(RedisCache.Constants.RootKey)]
    public class RedisCacheItem
    {
        [RedisKey]
        public String Key { get; set; }

        public DateTime Created { get; set; }
        public DateTime? LastAccessed { get; set; }
        public DateTime? Expiration { get; set; }
        public long? SlidingExpirationSeconds { get; set; }

        [RedisBinarySerialize]
        public Object Value { get; set; }

        public bool Locked { get; set; }
        public string LockedBy { get; set; }

        public long ValueSizeBytes { get; set; }
    }
}
