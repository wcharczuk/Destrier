﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Destrier.Redis.Core;

namespace Destrier.Redis.Cache
{
    public class RedisCache : IEnumerable<KeyValuePair<String, Object>>, IDisposable
    {
        public static class Constants
        {
            public const string RootKey = "Cache";
            public const string KeysKey = "53AFF8528106408F8856902181CE5345";
            public const string EphemeralKeysKey = "E0761FC136E34B669AEB36C1C2D3881A";
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

        public RedisCache() { }

        protected RedisClient _client = null;

        public RedisConnection Connection
        {
            get
            {
                return _client.Connection;
            }
        }

        public void Connect(RedisHostInfo host)
        {
            _client = new RedisClient(host);
        }

        public long Count 
        { 
            get 
            {
                return Keys.LongCount();
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
            _purgeExpiredKeys();
            var regular = _client.SetIsMember(Model.CreateKey(Constants.RootKey, Constants.KeysKey), key);
            var ephemeral = _client.SortedSetRank(Model.CreateKey(Constants.RootKey, Constants.KeysKey), key) != null;
            return regular || ephemeral;
        }

        public IEnumerable<String> Keys
        {
            get
            {
                _purgeExpiredKeys();
                var regularKeys = _client.SetMembers(Model.CreateKey(Constants.RootKey, Constants.KeysKey));
                var ephemeralKeys = _client.SortedSetRange(Model.CreateKey(Constants.RootKey, Constants.KeysKey), 0, -1); //-1 is the 'last element' index in a sorted set. see http://redis.io/commands/zrange
                return regularKeys.Concat(ephemeralKeys).Where(k => !String.IsNullOrEmpty(k));
            }
        }

        public void Add(String key, Object value, TimeSpan? slidingExpiration = null)
        {
            _addTrackedKey(key, slidingExpiration);
            var cacheItem = new RedisCacheItem();
            cacheItem.Key = key;
            cacheItem.Created = DateTime.UtcNow;
            cacheItem.Value = value;
            cacheItem.SlidingExpirationSeconds = slidingExpiration != null ? (long?)slidingExpiration.Value.TotalMilliseconds : null;
            cacheItem.ValueSizeBytes = Model.GetObjectSizeBytes(value);
            _client.SerializeObject(cacheItem, slidingExpiration: slidingExpiration);
        }

        public Object Get(String key)
        {
            var item = _getCacheItemWithKeyInternal(key);
            if (item != null)
            {
                if (item.SlidingExpirationSeconds != null)
                    _touchAsync(key);
                
                return item.Value;
            }
            else
                return null;
        }

        public Boolean Remove(String key)
        {
            var item = _getCacheItemWithKeyInternal(key);

            if (item != null)
            {
                _client.RemoveSerializedObject(item);
                _removeTrackedKey(key);
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
            if (!ContainsKey(key))
                Add(key, value, slidingExpiration);
            else
            {
                Set(key, value);
                if (slidingExpiration != null)
                    _touchAsync(key, slidingExpiration);
            }
        }

        public void Touch(String key, TimeSpan? newSlidingExpiration = null)
        {
            _touchAsync(key, newSlidingExpiration);
        }

        public void Clear()
        {
            _client.Remove(this.Keys.ToArray());
        }

        protected RedisCacheItem _getCacheItemWithKeyInternal(String key)
        {
            var compositeKey = Model.CreateKey(Constants.RootKey, key);
            return _client.DeserializeObject<RedisCacheItem>(compositeKey);
        }

        protected long? _getObjectSlidingExpriation(String key)
        {
            return _client.GetRawValue(Model.CreateKey(Constants.RootKey, key, Model.GetKeyForProperty<RedisCacheItem, long?>(ri => ri.SlidingExpirationSeconds))).LongValue;
        }

        protected void _addTrackedKey(String key, TimeSpan? slidingExpiration = null)
        {
            if (slidingExpiration != null)
            {
                long expiresUtc = RedisDataFormatUtil.ToUnixTimestamp(DateTime.UtcNow.Add(slidingExpiration.Value));
                _client.SortedSetAdd(Model.CreateKey(Constants.RootKey, RedisCache.Constants.EphemeralKeysKey), expiresUtc, key);
            }
            else
            {
                _client.SetAdd(Model.CreateKey(Constants.RootKey, RedisCache.Constants.KeysKey), key);
            }
        }

        protected void _removeTrackedKey(String key)
        {
            _client.SetRemove(Model.CreateKey(Constants.RootKey, Constants.KeysKey), key);
            _client.SortedSetRemove(Model.CreateKey(Constants.RootKey, Constants.EphemeralKeysKey), key);
        }

        protected void _purgeExpiredKeys()
        {
            _client.SortedSetRemoveRangeByScore(Model.CreateKey(Constants.RootKey, Constants.EphemeralKeysKey), 0, RedisDataFormatUtil.ToUnixTimestamp(DateTime.UtcNow));
        }

        protected void _purgeExpiredKeysAsync()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    using (var rc = new RedisClient(_client.AsRedisHostInfo()))
                    {
                        rc.SortedSetRemoveRangeByScore(Model.CreateKey(Constants.RootKey, Constants.EphemeralKeysKey), 0, RedisDataFormatUtil.ToUnixTimestamp(DateTime.UtcNow));
                    }
                }
                catch { }
            });
        }

        protected void _touchAsync(String key, TimeSpan? newSlidingExpiration = null)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    using (var rc = new RedisClient(_client.AsRedisHostInfo()))
                    {
                        long? newSlidingExpirationSeconds = newSlidingExpiration != null ? (long?)newSlidingExpiration.Value.TotalSeconds : null;
                        long? existingSlidingExpirationSeconds = rc.GetRawValue(Model.CreateKey(Constants.RootKey, key, Model.GetKeyForProperty<RedisCacheItem, long?>(ri => ri.SlidingExpirationSeconds))).LongValue;
                        long? slidingExpiration = newSlidingExpirationSeconds ?? existingSlidingExpirationSeconds;

                        if (slidingExpiration != null && slidingExpiration.Value != 0)
                        {
                            rc.Expire(Model.CreateKey(Constants.RootKey, Constants.KeysKey, key), (long)slidingExpiration);

                            var fullPrefix = Model.CreateKey(Constants.RootKey, key);
                            var members = ReflectionUtil.GetMemberMap(typeof(RedisCacheItem));
                            foreach (var member in members)
                            {
                                var fullKey = Model.CreateKey(fullPrefix, member.FullyQualifiedName);
                                rc.Expire(fullKey, slidingExpiration.Value);
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
