using Destrier.Redis.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Destrier.Redis.Cache
{
    public class RedisCacheLock : IDisposable
    {
        public RedisCacheLock(String key)
        {
            this.Key = key;
            this.Id = System.Guid.NewGuid().ToString("N");
        }

        public RedisCacheLock(String key, RedisConnection connection) : this(key)
        {
            this.Connection = connection;
        }

        public String Id { get; private set; }
        public String Key { get; private set; }

        public RedisConnection Connection { get; private set; }

        protected void _acquireLock()
        {
            var _client = new RedisClient(this.Connection);
        }

        protected void _releaseLock()
        {

        }

        public void Dispose()
        {
            //release lock.
        }
    }
}
