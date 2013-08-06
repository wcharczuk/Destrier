using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Destrier.Redis.Core;
using cmd = Destrier.Redis.Core.RedisCommandLiteral;

namespace Destrier.Redis
{
    public interface IListCommands
    {
        IEnumerable<String> ListPopBlockingLeft(Int32 timeout, String key, params String[] additionalKeys);
        IEnumerable<String> ListPopBlockingRight(Int32 timeout, String key, params String[] additionalKeys);
        void ListPopBlockingRightPushLeft(Int32 timeout, String source, String destination);
        String ListAtIndex(String key, long index);
        long ListInsert(String key, Boolean before, long pivot, String value);
        long ListLength(String key);
        String ListPopLeft(String key);
        long ListPushLeft(String key, String value, params String[] additionalValues);
        long ListPushLeftIfExists(String key, String value);
        IEnumerable<String> ListRange(String key, long start, long stop);
        long ListRemove(String key, long count, String value);
        void ListSet(String key, long index, String value);
        void ListTrim(String key, long start, long end);
        String ListPopRight(String key);
        String ListPopRightPushLeft(String source, String destionation);
        long ListPushRight(String key, String value, params String[] additionalValues);
        long ListPushRightIfExists(String key, String value);
    }

    public partial class RedisClient
    {
        public IEnumerable<String> ListPopBlockingLeft(Int32 timeout, String key, params String[] additionalKeys)
        {
            var args = new List<String>();
            args.Add(key);

            if(additionalKeys != null && additionalKeys.Any())
                args = args.Concat(additionalKeys).ToList();

            args.Add(timeout.ToString());

            _connection.Send(cmd.BLPOP, args.ToArray());
            return _connection.ReadMultiBulkReply().Select(r => r.ToString());
        }

        public IEnumerable<String> ListPopBlockingRight(Int32 timeout, String key, params String[] additionalKeys)
        {
            var args = new List<String>();
            args.Add(key);

            if (additionalKeys != null && additionalKeys.Any())
                args = args.Concat(additionalKeys).ToList();

            args.Add(timeout.ToString());

            _connection.Send(cmd.BRPOP, args.ToArray());
            return _connection.ReadMultiBulkReply().Select(r => r.ToString());
        }

        public void ListPopBlockingRightPushLeft(Int32 timeout, String source, String destination)
        {
            _connection.Send(cmd.BRPOPLPUSH, source, destination, timeout);
            _connection.ReadForError();
        }

        public String ListAtIndex(String key, long index)
        {
            _connection.Send(cmd.LINDEX, key, index);
            return _connection.ReadReply().ToString();
        }

        public long ListInsert(String key, Boolean before, long pivot, String value)
        {
            _connection.Send(cmd.LINDEX, key, before ? "BEFORE" : "AFTER", pivot, value);
            return _connection.ReadReply().LongValue.Value;
        }

        public long ListLength(String key)
        {
            _connection.Send(cmd.LLEN, key);
            return _connection.ReadReply().LongValue.Value;
        }

        public String ListPopLeft(String key)
        {
            _connection.Send(cmd.LPOP, key);
            return _connection.ReadReply().ToString();
        }

        public long ListPushLeft(String key, String value, params String[] additionalValues)
        {
            var args = new List<String>();
            args.Add(key);
            args.Add(value);

            if (additionalValues != null && additionalValues.Any())
                args = args.Concat(additionalValues).ToList();

            _connection.Send(cmd.LPUSH, args);
            return _connection.ReadReply().LongValue.Value;
        }

        public long ListPushLeftIfExists(String key, String value)
        {
            _connection.Send(cmd.LPUSHX, key, value);
            return _connection.ReadReply().LongValue.Value;
        }

        public IEnumerable<String> ListRange(String key, long start, long stop)
        {
            _connection.Send(cmd.LRANGE, start, stop);
            return _connection.ReadMultiBulkReply().Select(r => r.ToString());
        }

        public long ListRemove(String key, long count, String value)
        {
            _connection.Send(cmd.LREM, key, count, value);
            return _connection.ReadReply().LongValue.Value;
        }

        public void ListSet(String key, long index, String value)
        {
            _connection.Send(cmd.LSET, key, index, value);
            _connection.ReadForError();
        }

        public void ListTrim(String key, long start, long end)
        {
            _connection.Send(cmd.LTRIM, key, start, end);
            _connection.ReadForError();
        }

        public String ListPopRight(String key)
        {
            _connection.Send(cmd.RPOP, key);
            return _connection.ReadReply().ToString();
        }

        public String ListPopRightPushLeft(String source, String destionation)
        {
            _connection.Send(cmd.RPOPLPUSH, source, destionation);
            return _connection.ReadReply().ToString();
        }

        public long ListPushRight(String key, String value, params String[] additionalValues)
        {
            var args = new List<String>();
            args.Add(key);
            args.Add(value);

            if (additionalValues != null && additionalValues.Any())
                args = args.Concat(additionalValues).ToList();

            _connection.Send(cmd.RPUSH, args);
            return _connection.ReadReply().LongValue.Value;
        }

        public long ListPushRightIfExists(String key, String value)
        {
            _connection.Send(cmd.RPUSHNX, key, value);
            return _connection.ReadReply().LongValue.Value;
        }
    }
}
