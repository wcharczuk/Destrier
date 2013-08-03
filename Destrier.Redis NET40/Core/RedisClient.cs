using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Destrier.Redis.Core;

namespace Destrier.Redis.Core
{
    public class RedisClient : IDisposable
    {
        private RedisConnection _connection = null;

        public RedisClient(String host, int port = 6379, String password = null)
        {
            Host = host;
            Port = port;
            Password = password;
            _connection = RedisConnectionPool.GetConnection(host, port, password);
            _connection.Connect();
        }

        public RedisConnection Connection { get { return _connection; } }

        public String Host { get; set; }
        public Int32 Port { get; set; }
        public String Password { get; set; }

        public void MultiSet(IDictionary<String, String> values)
        {
            MultiSetRaw(values.ToDictionary(v => v.Key, v => Encoding.UTF8.GetBytes(v.Value)));
        }

        public void MultiSetRaw(IDictionary<String, Byte[]> values)
        {
            var data = new MemoryStream();
            foreach (var kvp in values)
            {
                var key = kvp.Key;
                var keyString = String.Format("{0}\r\n", key);
                var keyBuffer = Encoding.UTF8.GetBytes(keyString);

                var keyLengthString = String.Format("${0}\r\n", key.Length);
                var keyLengthBuffer = Encoding.UTF8.GetBytes(keyLengthString);

                var value = kvp.Value;
                var valueLengthString = String.Format("${0}\r\n", value.Length);
                var valueLengthStringBuffer = Encoding.UTF8.GetBytes(valueLengthString);

                data.Write(keyLengthBuffer, 0, keyLengthBuffer.Length);
                data.Write(keyBuffer, 0, keyBuffer.Length);

                data.Write(valueLengthStringBuffer, 0, valueLengthStringBuffer.Length);
                data.Write(value, 0, value.Length);
                data.Write(RedisConnection.EOL, 0, RedisConnection.EOL.Length);
            }
            _connection.SendCommand("*{0}\r\nMSET", values.Count * 2 + 1);
            _connection.SendData(data);
        }

        public Boolean Set(String key, String value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cannot be null or empty.", "key");

            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Cannot be null or empty.", "value");

            _connection.SendCommand("SET {0} {1}", key, value);
            return _connection.ReadData().IsSuccess;
        }

        public Boolean SetIfNotExists(String key, String value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cannot be null or empty.", "key");

            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Cannot be null or empty.", "value");

            _connection.SendCommand("SETNX {0} {1}", key, value);
            return _connection.ReadData().IsSuccess;
        }

        public String Get(String key)
        {
            return GetRawValue(key).StringValue;
        }

        public RedisDataValue GetRawValue(String key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cannot be null or empty.", "key");

            _connection.SendCommand("GET {0}", key);
            return _connection.ReadData();
        }

        public RedisKeyType KeyTypeOf(String key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cannot be null or empty.", "key");

            _connection.SendCommand("TYPE {0}", key);
            var keyType = _connection.ReadData().StringValue;
            switch (keyType)
            {
                case "none":
                    return RedisKeyType.None;
                case "string":
                    return RedisKeyType.String;
                case "set":
                    return RedisKeyType.Set;
                case "list":
                    return RedisKeyType.List;
                default:
                    throw new RedisException("Unknown Key Type Returned.");
            }
        }

        public Boolean ContainsKey(String key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cannot be null or empty.", "key");

            _connection.SendCommand("EXISTS {0}", key);
            return _connection.ReadData().LongValue.Equals(1);
        }

        public Boolean Remove(String key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cannot be null or empty.", "key");

            _connection.SendCommand("DEL {0}", key);
            return _connection.ReadData().LongValue.Equals(1);
        }

        public Boolean Remove(IEnumerable<String> keys)
        {
            if (keys == null || !keys.Any())
                throw new ArgumentException("Cannot be null or empty.", "key");

            _connection.SendCommand("DEL {0}", String.Join(",", keys));
            return _connection.ReadData().LongValue >= 1;
        }

        public String RandomKey()
        {
            _connection.SendCommand("RANDOMKEY");
            return _connection.ReadData().StringValue;
        }

        public IEnumerable<String> GetKeys(String pattern = "*")
        {
            _connection.SendCommand("KEYS {0}", pattern);
            var result = _connection.ReadData().StringValue;

            if (String.IsNullOrEmpty(result))
                return Enumerable.Empty<String>();

            return result.Split(' ');
        }

        public Boolean RenameKey(String from, String to)
        {
            if (string.IsNullOrEmpty(from))
                throw new ArgumentException("Cannot be null or empty.", "from");

            if (string.IsNullOrEmpty(to))
                throw new ArgumentException("Cannot be null or empty.", "to");

            _connection.SendCommand("RENAME {0} {1}", from, to);
            var result = _connection.ReadData().StringValue;
            return result.StartsWith("+");
        }

        public long Increment(String key, int? count = null)
        {
            if (count == null)
                _connection.SendCommand("INCR {0}", key);
            else
                _connection.SendCommand("INCRBY {0} {1}", key, count.Value);

            return _connection.ReadData().LongValue;
        }

        public long Decrement(String key, int? count = null)
        {
            if (count == null)
                _connection.SendCommand("DECR {0}", key);
            else
                _connection.SendCommand("DECRBY {0} {1}", key, count.Value);

            return _connection.ReadData().LongValue;
        }

        public bool Expire(String key, int seconds)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            _connection.SendCommand("EXPIRE {0} {1}", key, seconds);
            return _connection.ReadData().LongValue.Equals(1);
        }

        public bool ExpireAt(String key, DateTime time)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            var unixTimestamp = RedisDataFormat.ToUnixTimestamp(time);
            _connection.SendCommand("EXPIRE {0} {1}", key, unixTimestamp);
            return _connection.ReadData().LongValue.Equals(1);
        }

        public TimeSpan TimeToLive(String key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            _connection.SendCommand("TTL {0}", key);
            var result = _connection.ReadData().LongValue;
            return TimeSpan.FromSeconds((double)result);
        }

        public long GetDBSize()
        {
            _connection.SendCommand("DBSIZE");
            return _connection.ReadData().LongValue;
        }

        public void Save()
        {
            _connection.SendCommand("SAVE");
        }

        public void BackgroundSave()
        {
            _connection.SendCommand("BGSAVE");
        }

        public void Shutdown()
        {
            _connection.SendCommand("SHUTDOWN");
        }

        public void FlushAll()
        {
            _connection.SendCommand("FLUSHALL");
        }

        public void FlushDb()
        {
            _connection.SendCommand("FLUSHDB");
        }

        public DateTime GetLastSave()
        {
            _connection.SendCommand("LASTSAVE");
            return _connection.ReadData().DateTimeValue;
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.OnConnectionReleased();
                _connection = null;
            }
        }
    }
}
