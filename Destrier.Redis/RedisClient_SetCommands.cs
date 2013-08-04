using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cmd = Destrier.Redis.Core.RedisCommandLiteral;

namespace Destrier.Redis
{
    public interface ISetCommands
    {
        long SetAdd(String key, String member, params string[] additionalMembers);
        long SetCardinality(String key);
        IEnumerable<String> SetDiff(params String[] keys);
        long SetDiffStore(String destinationKey, String originKey, params string[] additionalKeys);
        IEnumerable<String> SetIntersect(params String[] keys);
        long SetIntersectStore(String destinationKey, String originKey, params string[] additionalKeys);
        Boolean SetIsMember(String key, String member);
        IEnumerable<String> SetMembers(String key);
        Boolean SetMove(String fromKey, String toKey, String member);
        String SetPop(String key);
        String SetRandomMember(String key);
        IEnumerable<String> SetRandomMember(String key, Int32 count);
        Boolean SetRemove(String key, String member, params String[] members);
        IEnumerable<String> SetUnion(params String[] keys);
        long SetUnionStore(String destinationKey, String originKey, params string[] additionalKeys);
    }

    partial class RedisClient : ISetCommands
    {
        public long SetAdd(String key, String member, params string[] additionalMembers)
        {
            if (additionalMembers == null || !additionalMembers.Any())
                _connection.Send(cmd.SADD, new String[] { key, member });
            else
                _connection.Send(cmd.SADD, new String[] { key, member }.Concat(additionalMembers).ToArray());
            
            return _connection.ReadReply().LongValue.Value;
        }

        public long SetCardinality(String key)
        {
            _connection.Send(cmd.SCARD, key);
            return _connection.ReadReply().LongValue.Value;
        }

        public IEnumerable<String> SetDiff(params String[] keys)
        {
            _connection.Send(cmd.SDIFF, keys);
            return _connection.ReadMultiBulkReply().Select(r => r.ToString());
        }

        public long SetDiffStore(String destinationKey, String originKey, params string[] additionalKeys)
        {
            if (additionalKeys == null || !additionalKeys.Any())
                _connection.Send(cmd.SDIFFSTORE, new String[] { destinationKey, originKey });
            else
                _connection.Send(cmd.SDIFFSTORE, new String[] { destinationKey, originKey }.Concat(additionalKeys).ToArray());
            return _connection.ReadReply().LongValue.Value;
        }

        public IEnumerable<String> SetIntersect(params String[] keys)
        {
            _connection.Send(cmd.SINTER, keys);
            return _connection.ReadMultiBulkReply().Select(r => r.ToString());
        }

        public long SetIntersectStore(String destinationKey, String originKey, params string[] additionalKeys)
        {
            if (additionalKeys == null || !additionalKeys.Any())
                _connection.Send(cmd.SINTERSTORE, new String[] { destinationKey, originKey });
            else
                _connection.Send(cmd.SINTERSTORE, new String[] { destinationKey, originKey }.Concat(additionalKeys).ToArray());

            return _connection.ReadReply().LongValue.Value;
        }

        public Boolean SetIsMember(String key, String member)
        {
            _connection.Send(cmd.SISMEMBER, key, member);
            return _connection.ReadReply().IsSuccess;
        }

        public IEnumerable<String> SetMembers(String key)
        {
            _connection.Send(cmd.SMEMBERS, key);
            return _connection.ReadMultiBulkReply().Select(r => r.ToString());
        }

        public Boolean SetMove(String fromKey, String toKey, String member)
        {
            _connection.Send(cmd.SMOVE, fromKey, toKey, member);
            return _connection.ReadReply().IsSuccess;
        }

        public String SetPop(String key)
        {
            _connection.Send(cmd.SPOP, key);
            return _connection.ReadReply().ToString();
        }

        public String SetRandomMember(String key)
        {
            _connection.Send(cmd.SRANDMEMBER, key);
            return _connection.ReadReply().ToString();
        }

        public IEnumerable<String> SetRandomMember(String key, Int32 count)
        {
            _connection.Send(cmd.SRANDMEMBER, key, count);
            return _connection.ReadMultiBulkReply().Select(r => r.ToString());
        }

        public Boolean SetRemove(String key, String member, params String[] members)
        {
            if (members != null && members.Any())
                _connection.Send(cmd.SREM, key, member);
            else
                _connection.Send(cmd.SREM, new String[] { key, member }.Concat(members).ToArray());

            return _connection.ReadReply().IsSuccess;
        }

        public IEnumerable<String> SetUnion(params String[] keys)
        {
            _connection.Send(cmd.SUNION, keys);
            return _connection.ReadMultiBulkReply().Select(v => v.ToString());
        }

        public long SetUnionStore(String destinationKey, String originKey, params string[] additionalKeys)
        {
            if (additionalKeys == null || !additionalKeys.Any())
                _connection.Send(cmd.SUNIONSTORE, new String[] { destinationKey, originKey });
            else
                _connection.Send(cmd.SUNIONSTORE, new String[] { destinationKey, originKey }.Concat(additionalKeys).ToArray());

            return _connection.ReadReply().LongValue.Value;
        }
    }
}
