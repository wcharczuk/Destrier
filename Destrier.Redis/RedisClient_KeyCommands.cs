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
        Boolean SetIfNotExists(String key, String value);
        void MultiSet(IDictionary<String, String> values);
        IEnumerable<String> MultiGet(params string[] args);
        String Get(String key);
        RedisKeyType KeyTypeOf(String key);
        Boolean ContainsKey(String key);
        Boolean Remove(String key);
        String RandomKey();
        IEnumerable<String> GetKeys(String pattern = "*");
        Boolean RenameKey(String from, String to);
        long Increment(String key, long? increment = null);
        long Decrement(String key, long? decrement = null);
        bool Expire(String key, long seconds);
        bool ExpireAt(String key, DateTime time);
        TimeSpan TimeToLive(String key);
        Boolean Persist(String key);
        Boolean Move(String key, int db);
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

        public Boolean SetBinary(String key, Stream value)
        {
            _connection.Send(cmd.SET, key, value);
            return _connection.ReadReply().IsSuccess;
        }

        public Boolean SetBinary(String key, Byte[] value)
        {
            _connection.Send(cmd.SET, key, value);
            return _connection.ReadReply().IsSuccess;
        }

        public Byte[] GetBinary(String key)
        {
            _connection.Send(cmd.GET, key);
            return _connection.ReadBinaryReply();
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

        public IEnumerable<String> MultiGet(params string[] args)
        {
            return MultiGetInternal(args).Select(r => r.ToString());
        }

        protected IEnumerable<RedisValue> MultiGetInternal(params string[] args)
        {
            if (args == null || !args.Any())
                throw new ArgumentException("Cannot be null or empty.", "args");

            _connection.Send(cmd.MGET, args);
            return _connection.ReadMultiBulkReply();
        }

        public String Get(String key)
        {
            return GetRawValue(key).ToString();
        }

        public RedisValue GetRawValue(String key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cannot be null or empty.", "key");

            _connection.Send(cmd.GET, key);
            return _connection.ReadReply();
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

        public Boolean ContainsKey(String key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cannot be null or empty.", "key");

            _connection.Send(cmd.EXISTS, key);
            return _connection.ReadReply().LongValue.Equals(1);
        }

        public Boolean Remove(String key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cannot be null or empty.", "key");

            _connection.Send(cmd.DEL, key);
            return _connection.ReadReply().LongValue.Equals(1);
        }

        public Boolean Remove(IEnumerable<String> keys)
        {
            if (keys == null || !keys.Any())
                throw new ArgumentException("Cannot be null or empty.", "keys");

            _connection.Send(cmd.DEL, keys.ToArray());
            return _connection.ReadReply().LongValue >= 1;
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

        public Boolean RenameKey(String from, String to)
        {
            if (string.IsNullOrEmpty(from))
                throw new ArgumentException("Cannot be null or empty.", "from");

            if (string.IsNullOrEmpty(to))
                throw new ArgumentException("Cannot be null or empty.", "to");

            _connection.Send(cmd.RENAME, from, to);
            return _connection.ReadReply().IsSuccess;
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

        public long Increment(String key, long? increment = null)
        {
            if (increment == null || (increment != null && increment.Value == 1))
                _connection.Send(cmd.INCR, key);
            else
                _connection.Send(cmd.INCRBY, key, increment.Value);

            return _connection.ReadReply().ToInt64();
        }

        public long Decrement(String key, long? decrement = null)
        {
            if (decrement == null || (decrement != null && decrement.Value == 1))
                _connection.Send(cmd.DECR, key);
            else
                _connection.Send(cmd.DECRBY, key, decrement.Value);

            return _connection.ReadReply().ToInt64();
        }

        public bool Expire(String key, long seconds)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            _connection.Send(cmd.EXPIRE, key, seconds);
            return _connection.ReadReply().IsSuccess;
        }

        public bool ExpireAt(String key, DateTime time)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            var unixTimestamp = RedisDataFormatUtil.ToUnixTimestamp(time);
            _connection.Send(cmd.EXPIREAT, key, unixTimestamp);
            return _connection.ReadReply().IsSuccess;
        }

        public bool ExpireMilliseconds(String key, long milliseconds)
        {
            _connection.Send(cmd.PEXPIRE, key, milliseconds);
            return _connection.ReadReply().IsSuccess;
        }

        public TimeSpan TimeToLive(String key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            _connection.Send(cmd.TTL, key);
            var result = _connection.ReadReply().LongValue;
            return TimeSpan.FromSeconds((double)result);
        }

        public TimeSpan TimeToLiveMilliseconds(String key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            _connection.Send(cmd.PTTL, key);
            var result = _connection.ReadReply().LongValue;
            return TimeSpan.FromMilliseconds((double)result);
        }

        public Boolean Persist(String key)
        {
            _connection.Send(cmd.PERSIST, key);
            return _connection.ReadReply().IsSuccess;
        }

        public Boolean Move(String key, int db)
        {
            _connection.Send(cmd.MOVE, key, db);
            return _connection.ReadReply().IsSuccess;
        }
    }
}
