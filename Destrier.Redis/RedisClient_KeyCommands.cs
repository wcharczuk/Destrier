using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Destrier.Redis.Core;
using cmd = Destrier.Redis.Core.RedisCommandLiteral;

namespace Destrier.Redis
{
    public interface IKeyCommands
    {
        Boolean Set(String key, String value);
        Boolean SetBinary(String key, Stream value);
        Task<Boolean> SetBinaryAsync(String key, Stream value);
        Boolean SetBinary(String key, Byte[] value);
        Task<Boolean> SetBinaryAsync(String key, Byte[] value);
        Task<Boolean> SetAsync(String key, String value);
        Boolean SetIfNotExists(String key, String value);
        Task<Boolean> SetIfNotExistsSetAsync(String key, String value);
        void MultiSet(IDictionary<String, String> values);
        String Get(String key);
        Task<String> GetAsync(String key);
        RedisValue GetRawValue(String key);
        Task<RedisValue> GetRawValueAsync(String key);
        Byte[] GetBinary(String key);
        Task<Byte[]> GetBinaryAsync(String key);
        IEnumerable<String> MultiGet(params string[] args);
        Task<IEnumerable<String>> MultiGetAsync(params string[] args);
        IEnumerable<RedisValue> MultiGetRawValues(params string[] args);
        Task<IEnumerable<RedisValue>> MultiGetRawValuesAsync(params string[] args);
        RedisKeyType KeyTypeOf(String key);
        Task<RedisKeyType> KeyTypeOfAsync(String key);
        Boolean Exists(String key);
        Task<Boolean> ExistsAsync(String key);
        Boolean Remove(params string[] keys);
        Task<Boolean> RemoveAsync(params string[] keys);
        String RandomKey();
        IEnumerable<String> GetKeys(String pattern = "*");
        Task<IEnumerable<String>> GetKeysAsync(String pattern = "*");
        Boolean RenameKey(String from, String to);
        Task<Boolean> RenameKeyAsync(String from, String to);
        long Increment(String key, long? increment = null);
        Task<long> IncrementAsync(String key, long? increment = null);
        long Decrement(String key, long? decrement = null);
        Task<long> DecrementAsync(String key, long? increment = null);
        Boolean Expire(String key, long seconds);
        Task<Boolean> ExpireAsync(String key, long seconds);
        Boolean ExpireMilliseconds(String key, long milliseconds);
        Task<Boolean> ExpireMillisecondsAsync(String key, long milliseconds);
        Boolean ExpireAt(String key, DateTime time);
        Task<Boolean> ExpireAtAsync(String key, DateTime time);
        TimeSpan TimeToLive(String key);
        Task<TimeSpan> TimeToLiveAsync(String key);
        TimeSpan TimeToLiveMilliseconds(String key);
        Task<TimeSpan> TimeToLiveMillisecondsAsync(String key);
        Boolean Persist(String key);
        Task<Boolean> PersistAsync(String key);
        Boolean Move(String key, int db);
        Task<Boolean> MoveAsync(String key, int db);
    }

    public partial class RedisClient : IKeyCommands
    {
        public Boolean Set(String key, String value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cannot be null or empty.", "key");

            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Cannot be null or empty.", "value");

            _connection.Send(cmd.SET, key, value);
            return _connection.ReadReply().IsSuccess;
        }

        public Task<Boolean> SetAsync(String key, String value)
        {
            return new Task<Boolean>(() => Set(key, value));
        }

        public Boolean SetBinary(String key, Stream value)
        {
            _connection.Send(cmd.SET, key, value);
            return _connection.ReadReply().IsSuccess;
        }

        public Task<Boolean> SetBinaryAsync(String key, Stream value)
        {
            return new Task<Boolean>(() => SetBinary(key, value));
        }

        public Boolean SetBinary(String key, Byte[] value)
        {
            _connection.Send(cmd.SET, key, value);
            return _connection.ReadReply().IsSuccess;
        }

        public Task<Boolean> SetBinaryAsync(String key, Byte[] value)
        {
            return new Task<Boolean>(() => SetBinary(key, value));
        }

        public Boolean SetIfNotExists(String key, String value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cannot be null or empty.", "key");

            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Cannot be null or empty.", "value");

            _connection.Send(cmd.SETNX, key, value);
            return _connection.ReadReply().IsSuccess;
        }

        public Task<Boolean> SetIfNotExistsSetAsync(String key, String value)
        {
            return new Task<Boolean>(() => SetIfNotExists(key, value));
        }

        public void MultiSet(IDictionary<String, String> values)
        {
            var data = new List<String>();
            foreach (var kvp in values)
            {
                data.Add(kvp.Key);
                data.Add(kvp.Value);
            }
            _connection.Send(cmd.MSET, data.ToArray());
            _connection.ReadForError();
        }

        public Task MultiSetAsync(IDictionary<String, String> values)
        {
            return new Task(() => MultiSet(values));
        }

        public Byte[] GetBinary(String key)
        {
            _connection.Send(cmd.GET, key);
            return _connection.ReadBinaryReply();
        }

        public Task<Byte[]> GetBinaryAsync(String key)
        {
            return new Task<Byte[]>(() => GetBinary(key));
        }
        
        public IEnumerable<String> MultiGet(params string[] args)
        {
            return MultiGetRawValues(args).Select(r => r.ToString());
        }

        public Task<IEnumerable<String>> MultiGetAsync(params string[] args)
        {
            return new Task<IEnumerable<string>>(() => MultiGet(args));
        }

        public IEnumerable<RedisValue> MultiGetRawValues(params string[] args)
        {
            if (args == null || !args.Any())
                throw new ArgumentException("Cannot be null or empty.", "args");

            _connection.Send(cmd.MGET, args);
            return _connection.ReadMultiBulkReply();
        }

        public Task<IEnumerable<RedisValue>> MultiGetRawValuesAsync(params string[] args)
        {
            return new Task<IEnumerable<RedisValue>>(() => MultiGetRawValues(args));
        }

        public String Get(String key)
        {
            return GetRawValue(key).ToString();
        }

        public Task<String> GetAsync(String key)
        {
            return new Task<String>(() => Get(key));
        }

        public RedisValue GetRawValue(String key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cannot be null or empty.", "key");

            _connection.Send(cmd.GET, key);
            return _connection.ReadReply();
        }

        public Task<RedisValue> GetRawValueAsync(String key)
        {
            return new Task<RedisValue>(() => GetRawValue(key));
        }

        public RedisKeyType KeyTypeOf(String key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cannot be null or empty.", "key");

            _connection.Send(cmd.TYPE, key);
            var keyType = _connection.ReadReply().StringValue;
            switch (keyType)
            {
                case "none": //tbd if this actually gets used.
                    return RedisKeyType.None;
                case "string":
                    return RedisKeyType.String;
                case "set":
                    return RedisKeyType.Set;
                case "list":
                    return RedisKeyType.List;
                case "zset":
                    return RedisKeyType.ZSet;
                case "hash":
                    return RedisKeyType.Hash;
                default:
                    throw new RedisException("Unknown Key Type Returned.");
            }
        }

        public Task<RedisKeyType> KeyTypeOfAsync(String key)
        {
            return new Task<RedisKeyType>(() => KeyTypeOf(key));
        }

        public Boolean Exists(String key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cannot be null or empty.", "key");

            _connection.Send(cmd.EXISTS, key);
            return _connection.ReadReply().LongValue.Equals(1);
        }

        public Task<Boolean> ExistsAsync(String key)
        {
            return new Task<Boolean>(() => Exists(key));
        }

        public Boolean Remove(params string[] keys)
        {
            if (keys == null || keys.Length == 0)
                throw new ArgumentException("Cannot be null or empty.", "keys");

            var args = keys.Select(k => k as object).ToArray();

            _connection.Send(cmd.DEL, args);
            return _connection.ReadReply().IsSuccess;
        }

        public Task<Boolean> RemoveAsync(params string[] keys)
        {
            return new Task<Boolean>(() => Remove(keys));
        }

        public String RandomKey()
        {
            _connection.Send(cmd.RANDOMKEY);
            return _connection.ReadReply().StringValue;
        }

        public IEnumerable<String> GetKeys(String pattern = "*")
        {
            _connection.Send(cmd.KEYS, pattern);
            return _connection.ReadMultiBulkReply().Select(r => r.ToString());
        }

        public Task<IEnumerable<String>> GetKeysAsync(String pattern = "*")
        {
            return new Task<IEnumerable<String>>(() => GetKeys(pattern));
        }

        public Boolean RenameKey(String from, String to)
        {
            if (string.IsNullOrEmpty(from))
                throw new ArgumentException("Cannot be null or empty.", "from");

            if (string.IsNullOrEmpty(to))
                throw new ArgumentException("Cannot be null or empty.", "to");

            _connection.Send(cmd.RENAME, from, to);
            return _connection.ReadReply().IsSuccess;
        }

        public Task<Boolean> RenameKeyAsync(String from, String to)
        {
            return new Task<Boolean>(() => RenameKey(from, to));
        }

        public Boolean RenameKeyIfNotExists(String from, String to)
        {
            if (string.IsNullOrEmpty(from))
                throw new ArgumentException("Cannot be null or empty.", "from");

            if (string.IsNullOrEmpty(to))
                throw new ArgumentException("Cannot be null or empty.", "to");

            _connection.Send(cmd.RENAMENX, from, to);
            return _connection.ReadReply().IsSuccess;
        }

        public Task<Boolean> RenameKeyIfNotExistsAsync(String from, String to)
        {
            return new Task<Boolean>(() => RenameKeyIfNotExists(from, to));
        }

        public long Increment(String key, long? increment = null)
        {
            if (increment == null || (increment != null && increment.Value == 1))
                _connection.Send(cmd.INCR, key);
            else
                _connection.Send(cmd.INCRBY, key, increment.Value);

            return _connection.ReadReply().ToInt64();
        }

        public Task<long> IncrementAsync(String key, long? increment = null)
        {
            return new Task<long>(() => Increment(key, increment));
        }

        public long Decrement(String key, long? decrement = null)
        {
            if (decrement == null || (decrement != null && decrement.Value == 1))
                _connection.Send(cmd.DECR, key);
            else
                _connection.Send(cmd.DECRBY, key, decrement.Value);

            return _connection.ReadReply().ToInt64();
        }

        public Task<long> DecrementAsync(String key, long? increment = null)
        {
            return new Task<long>(() => Decrement(key, increment));
        }

        public Boolean Expire(String key, long seconds)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            _connection.Send(cmd.EXPIRE, key, seconds);
            return _connection.ReadReply().IsSuccess;
        }

        public Task<Boolean> ExpireAsync(String key, long seconds)
        {
            return new Task<Boolean>(() => Expire(key, seconds));
        }

        public Boolean ExpireAt(String key, DateTime time)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            var unixTimestamp = RedisDataFormatUtil.ToUnixTimestamp(time);
            _connection.Send(cmd.EXPIREAT, key, unixTimestamp);
            return _connection.ReadReply().IsSuccess;
        }

        public Task<Boolean> ExpireAtAsync(String key, DateTime time)
        {
            return new Task<Boolean>(() => ExpireAt(key, time));
        }

        public Boolean ExpireMilliseconds(String key, long milliseconds)
        {
            _connection.Send(cmd.PEXPIRE, key, milliseconds);
            return _connection.ReadReply().IsSuccess;
        }

        public Task<Boolean> ExpireMillisecondsAsync(String key, long milliseconds)
        {
            return new Task<Boolean>(() => ExpireMilliseconds(key, milliseconds));
        }

        public TimeSpan TimeToLive(String key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            _connection.Send(cmd.TTL, key);
            var result = _connection.ReadReply().LongValue;
            return TimeSpan.FromSeconds((double)result);
        }

        public Task<TimeSpan> TimeToLiveAsync(String key)
        {
            return new Task<TimeSpan>(() => TimeToLive(key));
        }

        public TimeSpan TimeToLiveMilliseconds(String key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            _connection.Send(cmd.PTTL, key);
            var result = _connection.ReadReply().LongValue;
            return TimeSpan.FromMilliseconds((double)result);
        }

        public Task<TimeSpan> TimeToLiveMillisecondsAsync(String key)
        {
            return new Task<TimeSpan>(() => TimeToLiveMilliseconds(key));
        }

        public Boolean Persist(String key)
        {
            _connection.Send(cmd.PERSIST, key);
            return _connection.ReadReply().IsSuccess;
        }

        public Task<Boolean> PersistAsync(String key)
        {
            return new Task<Boolean>(() => Persist(key));
        }

        public Boolean Move(String key, int db)
        {
            _connection.Send(cmd.MOVE, key, db);
            return _connection.ReadReply().IsSuccess;
        }

        public Task<Boolean> MoveAsync(String key, int db)
        {
            return new Task<Boolean>(() => Move(key, db));
        }
    }
}
