using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Destrier.Redis.Core;

namespace Destrier.Redis.Caching
{
    //notes: race conditions abound.
    public class RedisCache : IEnumerable<KeyValuePair<String, Object>>, IDisposable
    {
        public static class Constants
        {
            public const string RootKey = "Cache";

            public const string CountKey = "A2799DC5EBED4A6F90C02AECEA16E5AC";
            public const string SizeKey = "22DE8EF431CF4EEB949D861C3B28200D";
            public const string KeysKey = "53AFF8528106408F8856902181CE5345";
        }

        private static object _lock = new object();
        private static RedisCache _current = null;
        public static RedisCache Current
        {
            get
            {
                if (_current == null)
                    lock(_lock)
                        if(_current == null)
                            _current = new RedisCache();

                return _current;
            }
        }

        protected RedisClient _client = null;

        public void Connect(RedisHostInfo host)
        {
            _client = new RedisClient(host);
        }

        public long Count 
        { 
            get 
            {
                return _client.GetRawValue(Model.CreateKey(Constants.RootKey, Constants.CountKey)).ToInt64();
            }
            private set
            {
                _client.Set(Model.CreateKey(Constants.RootKey, Constants.CountKey), value.ToString());
            }
        }

        public Object this[String key]
        {
            get
            {
                return Get(key);
            }
            set
            {
                SetOrAdd(key, value);
            }
        }

        public Boolean ContainsKey(String key)
        {
            return _client.GetKeys(Model.CreateKey(Constants.RootKey, Constants.KeysKey, key)).Any();
        }

        public IEnumerable<String> Keys
        {
            get
            {
                return _client.GetKeys(Model.CreateKey(Constants.RootKey, Constants.KeysKey));
            }
        }

        public void Add(String key, Object value, TimeSpan? slidingExpiration = null)
        {
            _addTrackedKey(key, slidingExpiration);
            var cacheItem = new RedisCacheItem();
            cacheItem.Key = key;
            cacheItem.Created = DateTime.UtcNow;
            cacheItem.Value = value;
            cacheItem.SlidingExpirationMilliseconds = slidingExpiration != null ? (long?)slidingExpiration.Value.TotalMilliseconds : null;
            _client.SerializeObject(value, keyPrefix: Constants.RootKey, slidingExpiration: slidingExpiration);
        }

        public Object Get(String key)
        {
            var item = _getCacheItemWithKeyInternal(key);
            if (item != null)
            {
                _touchAsync(key);
                return item.Value;
            }
            else
                return null;
        }

        public Boolean Remove(String key)
        {
            var item = Get(key);

            if (item != null)
            {
                _client.RemoveSerializedObject(item);
                return true;
            }
            return false;
        }

        public void Set(String key, Object value)
        {
            if (!ContainsKey(key))
                throw new KeyNotFoundException();

            _touchAsync(key);
            var memberKey = Model.GetKeyForProperty<RedisCacheItem, Object>(ri => ri.Value);
            var compositeKey = Model.CreateKey(Constants.RootKey, key, memberKey);

            _client.BinarySerializeObject(compositeKey, value);
        }

        public void SetOrAdd(String key, Object value, TimeSpan? slidingExpiration = null)
        {
            if(!ContainsKey(key))
                Add(key, value, slidingExpiration);
            else
                Set(key, value);
        }

        protected RedisCacheItem _getCacheItemWithKeyInternal(String key)
        {
            var compositeKey = Model.CreateKey(Constants.RootKey, key);
            return _client.DeserializeObject<RedisCacheItem>(compositeKey);
        }

        protected long? _getObjectSlidingExpriation(String key)
        {
            return _client.GetRawValue(Model.CreateKey(Constants.RootKey, key, Model.GetKeyForProperty<RedisCacheItem, long?>(ri => ri.SlidingExpirationMilliseconds))).LongValue;
        }

        protected void _addTrackedKey(String key, TimeSpan? slidingExpiration = null)
        {
            var compositeKey = Model.CreateKey(Constants.RootKey, Constants.KeysKey);

            _client.Set(compositeKey, key);

            if(slidingExpiration != null)
                _client.ExpireMilliseconds(compositeKey, (long)slidingExpiration.Value.TotalMilliseconds);
        }

        protected void _removeTrackedKey(String key)
        {
            var compositeKey = Model.CreateKey(Constants.RootKey, Constants.KeysKey);
            _client.SortedSetRemove(compositeKey, key);
        }

        protected void _touchAsync(String key)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    using (var rc = new RedisClient(_client.AsRedisHostInfo()))
                    {
                        long? slidingExpiration = rc.GetRawValue(Model.CreateKey(Constants.RootKey, key, Model.GetKeyForProperty<RedisCacheItem, long?>(ri => ri.SlidingExpirationMilliseconds))).LongValue;
                        if (slidingExpiration != null && slidingExpiration.Value != 0)
                        {
                            rc.ExpireMilliseconds(Model.CreateKey(Constants.RootKey, Constants.KeysKey, key), (long)slidingExpiration);

                            var fullPrefix = Model.CreateKey(Constants.RootKey, key);
                            var members = ReflectionUtil.GetMemberMap(typeof(RedisCacheItem));
                            foreach (var member in members)
                            {
                                var fullKey = Model.CreateKey(fullPrefix, member.FullyQualifiedName);
                                rc.ExpireMilliseconds(fullKey, slidingExpiration.Value);
                            }
                        }
                    }
                }
                catch { }
            });
        }

        public void Dispose()
        {
            if (_client != null)
                _client.Dispose();
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return new CacheEnumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new CacheEnumerator(this);
        }

        public class CacheEnumerator : IEnumerator<KeyValuePair<string, object>>
        {
            public CacheEnumerator(RedisCache cache)
                : base()
            {
                Cache = cache;

                _keyEnumerator = cache.Keys.GetEnumerator();
            }

            public RedisCache Cache { get; set; }

            private IEnumerator<String> _keyEnumerator { get; set; }

            private String _currentKey = null;

            public KeyValuePair<string, object> Current
            {
                get 
                {
                    if(!String.IsNullOrEmpty(_currentKey))
                    {
                        var item = Cache.Get(_currentKey);
                        return new KeyValuePair<string, object>(_currentKey, item);
                    }
                    return new KeyValuePair<string,object>();
                }
            }

            public void Dispose() { }

            object System.Collections.IEnumerator.Current
            {
                get { return this.Current; }
            }

            public bool MoveNext()
            {
                var valid = _keyEnumerator.MoveNext();
                if (valid)
                    _currentKey = _keyEnumerator.Current;

                return valid;
            }

            public void Reset()
            {
                _currentKey = null;
                _keyEnumerator.Reset();
            }
        }
    }
}
