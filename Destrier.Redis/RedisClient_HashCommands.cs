using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cmd = Destrier.Redis.Core.RedisCommandLiteral;

namespace Destrier.Redis
{
    public interface IHashCommands
    {
        long HashDelete(String key, String field, params String[] additionalFields);
        Boolean HashExists(String key, String field);
        String HashGet(String key, String field);
        IDictionary<String, String> HashGetAll(String key);
        long HashIncrementBy(String key, String field, long increment);
        float HashIncrementByFloat(String key, String field, float increment);
        IEnumerable<String> HashKeys(String key);
        long HashLength(String key);
        IEnumerable<String> HashMultiGet(String key, String field, params String[] additionalFields);
        void HashMultiSet(String key, IDictionary<String, String> values);
        Boolean HashSet(String key, String field, String value);
        Boolean HashSetIfNotExists(String key, String field, String value);
        IEnumerable<String> HashValues(String key);
    }

    public partial class RedisClient : IHashCommands
    {
        public long HashDelete(String key, String field, params String[] additionalFields)
        {
            var args = new List<String>();
            args.Add(key);
            args.Add(field);
            if (additionalFields != null && additionalFields.Any())
                args = args.Concat(additionalFields).ToList();

            _connection.Send(cmd.HDEL, args);
            return _connection.ReadReply().ToInt64();
        }

        public Boolean HashExists(String key, String field)
        {
            _connection.Send(cmd.HEXISTS, key, field);
            return _connection.ReadReply().IsSuccess;
        }

        public String HashGet(String key, String field)
        {
            _connection.Send(cmd.HGET, key, field);
            return _connection.ReadReply().ToString();
        }

        public IDictionary<String, String> HashGetAll(String key)
        {
            _connection.Send(cmd.HGETALL, key);
            IEnumerable<String> results = _connection.ReadMultiBulkReply().Select(r => r.ToString());

            var dict = new Dictionary<String, String>();

            var enumerator = results.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var keyValue = enumerator.Current;
                enumerator.MoveNext();
                var value = enumerator.Current;
                dict.Add(keyValue, value);
            }

            return dict;
        }

        public long HashIncrementBy(String key, String field, long increment)
        {
            _connection.Send(cmd.HINCRBY, key, field, increment);
            return _connection.ReadReply().ToInt64();
        }

        public float HashIncrementByFloat(String key, String field, float increment)
        {
            _connection.Send(cmd.HINCRBY, key, field, increment);
            return _connection.ReadReply().ToSingle();
        }

        public IEnumerable<String> HashKeys(String key)
        {
            _connection.Send(cmd.HKEYS, key);
            return _connection.ReadMultiBulkReply().Select(r => r.ToString());
        }

        public long HashLength(String key)
        {
            _connection.Send(cmd.HLEN, key);
            return _connection.ReadReply().ToInt64();
        }

        public IEnumerable<String> HashMultiGet(String key, String field, params String[] additionalFields)
        {
            var args = new List<String>();
            args.Add(key);
            args.Add(field);
            if (additionalFields != null && additionalFields.Any())
                args = args.Concat(additionalFields).ToList();

            _connection.Send(cmd.HMGET, args);
            return _connection.ReadMultiBulkReply().Select(r => r.ToString());
        }

        public void HashMultiSet(String key, IDictionary<String, String> values)
        {
            var data = new List<String>();
            data.Add(key);
            foreach (var kvp in values)
            {
                data.Add(kvp.Key);
                data.Add(kvp.Value);
            }
            _connection.Send(cmd.HMSET, data.ToArray());
            _connection.ReadForError();
        }

        public Boolean HashSet(String key, String field, String value)
        {
            _connection.Send(cmd.HSET, key, field, value);
            return _connection.ReadReply().IsSuccess;
        }

        public Boolean HashSetIfNotExists(String key, String field, String value)
        {
            _connection.Send(cmd.HSETNX, key, field, value);
            return _connection.ReadReply().IsSuccess;
        }

        public IEnumerable<String> HashValues(String key)
        {
            _connection.Send(cmd.HVALS, key);
            return _connection.ReadMultiBulkReply().Select(r => r.ToString());
        }
    }
}
