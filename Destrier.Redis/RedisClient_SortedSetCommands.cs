using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cmd = Destrier.Redis.Core.RedisCommandLiteral;

namespace Destrier.Redis
{
    public interface ISortedSetCommands
    {
        long SortedSetAdd(String key, long score, String member, params Tuple<long, string>[] additionalValues);
        long SortedSetCardinality(String key);
        long SortedSetCount(String key, long min, long max);
        long SortedSetIncrementBy(String key, long increment, String member);
        long SortedSetIntersectStore(String destination, String key, IEnumerable<String> additionalKeys = null, IEnumerable<long> weights = null, String aggregate = null);
        IEnumerable<String> SortedSetRange(String key, long start, long stop, Boolean withScores = false);
        IEnumerable<String> SortedSetRangeByScore(String key, long min, long max, Boolean withScores = false, long? offset = null, long? count = null);
        long? SortedSetRank(String key, String member);
        long SortedSetRemove(String key, String member, params String[] additionalMembers);
        long SortedSetRemoveRangeByRank(String key, long start, long stop);
        long SortedSetRemoveRangeByScore(String key, long min, long max);
        IEnumerable<String> SortedSetReverseRange(String key, long start, long stop, Boolean withScores = false);
        IEnumerable<String> SortedSetReverseRangeByScore(String key, long max, long min, Boolean withScores = false, long? offset = null, long? count = null);
        long? SortedSetReverseRank(String key, String member);
        long SortedSetScore(String key, String member);
        long SortedSetUnionStore(String destination, String key, IEnumerable<String> additionalKeys = null, IEnumerable<long> weights = null, String aggregate = null);
    }

    public partial class RedisClient : ISortedSetCommands
    {
        public long SortedSetAdd(String key, long score, String member, params Tuple<long, string>[] additionalValues)
        {
            var args = new List<string>();
            args.Add(key);
            args.Add(score.ToString());
            args.Add(member);

            if (additionalValues != null && additionalValues.Any())
            {
                foreach (var v in additionalValues)
                {
                    args.Add(v.Item1.ToString());
                    args.Add(v.Item2);
                }
            }

            _connection.Send(cmd.ZADD, args.ToArray());
            return _connection.ReadReply().ToInt64();
        }

        public long SortedSetCardinality(String key)
        {
            _connection.Send(cmd.ZCARD, key);
            return _connection.ReadReply().ToInt64();
        }

        public long SortedSetCount(String key, long min, long max)
        {
            _connection.Send(cmd.ZCOUNT, key, min, max);
            return _connection.ReadReply().ToInt64();
        }

        public long SortedSetIncrementBy(String key, long increment, String member)
        {
            _connection.Send(cmd.ZINCRBY, key, increment, member);
            return _connection.ReadReply().ToInt64();
        }

        public long SortedSetIntersectStore(String destination, String key, IEnumerable<String> additionalKeys = null, IEnumerable<long> weights = null, String aggregate = null)
        {
            var args = new List<string>();

            long numKeys = 1;
            if (additionalKeys != null && additionalKeys.Any())
                foreach (var k in additionalKeys)
                {
                    args.Add(k);
                    numKeys++;
                }

            if (weights != null && weights.Any())
            {
                args.Add("WEIGHTS");
                foreach (var w in weights)
                    args.Add(w.ToString());
            }

            if (!String.IsNullOrEmpty(aggregate))
            {
                args.Add("AGGREGATE");
                args.Add(aggregate);
            }

            _connection.Send(cmd.ZINTERSTORE, args);
            return _connection.ReadReply().ToInt64();
        }

        public IEnumerable<String> SortedSetRange(String key, long start, long stop, Boolean withScores = false)
        {
            if (withScores)
                _connection.Send(cmd.ZRANGE, key, start, stop, "WITHSCORES");
            else
                _connection.Send(cmd.ZRANGE, key, start, stop);

            return _connection.ReadMultiBulkReply().Select(r => r.ToString());
        }

        public IEnumerable<String> SortedSetRangeByScore(String key, long min, long max, Boolean withScores = false, long? offset = null, long? count = null)
        {
            var args = new List<String>();
            args.Add(key);
            args.Add(min.ToString());
            args.Add(max.ToString());

            if (withScores)
                args.Add("WITHSCORES");

            if (offset != null)
            {
                args.Add("LIMIT");
                args.Add(offset.Value.ToString());
                args.Add(count.Value.ToString());
            }

            _connection.Send(cmd.ZRANGEBYSCORE, args.ToArray());
            return _connection.ReadMultiBulkReply().Select(r => r.ToString());
        }

        public long? SortedSetRank(String key, String member)
        {
            _connection.Send(cmd.ZRANK, key, member);
            return _connection.ReadReply().LongValue;
        }

        public long SortedSetRemove(String key, String member, params String[] additionalMembers)
        {
            var args = new List<String>();
            args.Add(key);
            args.Add(member);
            if (additionalMembers != null && additionalMembers.Any())
                args = args.Concat(additionalMembers).ToList();

            _connection.Send(cmd.ZREM, args.ToArray());
            return _connection.ReadReply().ToInt64();
        }

        public long SortedSetRemoveRangeByRank(String key, long start, long stop)
        {
            _connection.Send(cmd.ZREMRANGEBYRANK, key, start, stop);
            return _connection.ReadReply().ToInt64();
        }

        public long SortedSetRemoveRangeByScore(String key, long min, long max)
        {
            _connection.Send(cmd.ZREMRANGEBYSCORE, key, min, max);
            return _connection.ReadReply().ToInt64();
        }

        public IEnumerable<String> SortedSetReverseRange(String key, long start, long stop, Boolean withScores = false)
        {
            if (withScores)
            {
                _connection.Send(cmd.ZREVRANGE, key, start, stop, "WITHSCORES");
                return _connection.ReadMultiBulkReply().Select(r => r.ToString());
            }
            else
            {
                _connection.Send(cmd.ZREVRANGE, key, start, stop);
                return _connection.ReadMultiBulkReply().Select(r => r.ToString());
            }
        }

        public IEnumerable<String> SortedSetReverseRangeByScore(String key, long max, long min, Boolean withScores = false, long? offset = null, long? count = null)
        {
            var args = new List<String>();
            args.Add(key);
            args.Add(max.ToString());
            args.Add(min.ToString());

            if (withScores)
                args.Add("WITHSCORES");

            if (offset != null)
            {
                args.Add("LIMIT");
                args.Add(offset.Value.ToString());
                args.Add(count.Value.ToString());
            }

            _connection.Send(cmd.ZREVRANGEBYSCORE, args.ToArray());
            return _connection.ReadMultiBulkReply().Select(r => r.ToString());
        }

        public long? SortedSetReverseRank(String key, String member)
        {
            _connection.Send(cmd.ZREVRANK, key, member);
            return _connection.ReadReply().LongValue;
        }

        public long SortedSetScore(String key, String member)
        {
            _connection.Send(cmd.ZSCORE, key, member);
            return _connection.ReadReply().ToInt64();
        }

        public long SortedSetUnionStore(String destination, String key, IEnumerable<String> additionalKeys = null, IEnumerable<long> weights = null, String aggregate = null)
        {
            var args = new List<string>();

            long numKeys = 1;
            if (additionalKeys != null && additionalKeys.Any())
                foreach (var k in additionalKeys)
                {
                    args.Add(k);
                    numKeys++;
                }

            if (weights != null && weights.Any())
            {
                args.Add("WEIGHTS");
                foreach (var w in weights)
                    args.Add(w.ToString());
            }

            if (!String.IsNullOrEmpty(aggregate))
            {
                args.Add("AGGREGATE");
                args.Add(aggregate);
            }

            _connection.Send(cmd.ZUNIONSTORE, args.ToArray());
            return _connection.ReadReply().ToInt64();
        }
    }
}
