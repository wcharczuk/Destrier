﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Destrier.Redis.Core
{
    public static class RedisCommandLiteral
    {
        public static readonly string AUTH = "AUTH";

        public static readonly string BGSAVE = "BGSAVE";
        public static readonly string BLPOP = "BLPOP";
        public static readonly string BRPOP = "BRPOP";
        public static readonly string BRPOPLPUSH = "BRPOPLPUSH";

        public static readonly string CLIENT = "CLIENT";
        public static readonly string CONFIG = "CONFIG";

        public static readonly string DBSIZE = "DBSIZE";
        public static readonly string DECR = "DECR";
        public static readonly string DECRBY = "DECRBY";
        public static readonly string DEL = "DEL";
        public static readonly string DISCARD = "DISCARD";

        public static readonly string ECHO = "ECHO";
        public static readonly string EXISTS = "EXISTS";
        public static readonly string EXPIRE = "EXPIRE";
        public static readonly string EXPIREAT = "EXPIREAT";
        public static readonly string EXEC = "EXEC";

        public static readonly string FLUSHALL = "FLUSHALL";
        public static readonly string FLUSHDB = "FLUSHDB";
        
        public static readonly string GET = "GET";
        public static readonly string GETNAME = "GETNAME";

        public static readonly string INCR = "INCR";
        public static readonly string INCRBY = "INCRBY";

        public static readonly string HDEL = "HDEL";
        public static readonly string HEXISTS = "HEXISTS";
        public static readonly string HGET = "HGET";
        public static readonly string HGETALL = "HGETALL";
        public static readonly string HINCRBY = "HINCRBY";
        public static readonly string HINCRBYFLOAT = "HINCRBYFLOAT";
        public static readonly string HKEYS = "HKEYS";
        public static readonly string HLEN = "HLEN";
        public static readonly string HMGET = "HMGET";
        public static readonly string HMSET = "HMSET";
        public static readonly string HSET = "HSET";
        public static readonly string HSETNX = "HSETNX";
        public static readonly string HVALS = "HVALS";

        public static readonly string KILL = "KILL";
        public static readonly string KEYS = "KEYS";

        public static readonly string LIST = "LIST";
        public static readonly string LASTSAVE = "LASTSAVE";

        public static readonly string LINDEX = "LINDEX";
        public static readonly string LINSERT = "LINSERT";
        public static readonly string LLEN = "LLEN";
        public static readonly string LPOP = "LPOP";
        public static readonly string LPUSH = "LPUSH";
        public static readonly string LPUSHX = "LPUSHX";
        public static readonly string LRANGE = "LRANGE";
        public static readonly string LREM = "LREM";
        public static readonly string LSET = "LSET";
        public static readonly string LTRIM = "LTRIM";
        
        public static readonly string MOVE = "MOVE";
        public static readonly string MGET = "MGET";
        public static readonly string MSET = "MSET";
        public static readonly string MULTI = "MULTI";

        public static readonly string PERSIST = "PERSIST";
        public static readonly string PING = "PING";
        public static readonly string PTTL = "PTTL";
        public static readonly string PEXPIRE = "PEXPIRE";
        public static readonly string PEXPIREAT = "PEXPIREAT";

        public static readonly string QUIT = "QUIT";

        public static readonly string RANDOMKEY = "RANDOMKEY";
        public static readonly string RENAME = "RENAME";
        public static readonly string RENAMENX = "RENAMENX";
        public static readonly string REWRITE = "REWRITE";
        public static readonly string RESETSTAT = "RESETSTAT";

        public static readonly string RPOP = "RPOP";
        public static readonly string RPOPLPUSH = "RPOPLPUSH";
        public static readonly string RPUSH = "RPUSH";
        public static readonly string RPUSHNX = "RPUSHNX";

        public static readonly string SADD = "SADD";
        public static readonly string SCARD = "SCARD";
        public static readonly string SDIFF = "SDIFF";
        public static readonly string SDIFFSTORE = "SDIFFSTORE";
        public static readonly string SINTER = "SINTER";
        public static readonly string SINTERSTORE = "SINTERSTORE";
        public static readonly string SISMEMBER = "SISMEMBER";
        public static readonly string SMEMBERS = "SMEMBERS";
        public static readonly string SMOVE = "SMOVE";
        public static readonly string SPOP = "SPOP";
        public static readonly string SRANDMEMBER = "SRANDMEMBER";
        public static readonly string SREM = "SREM";
        public static readonly string SUNION = "SUNION";
        public static readonly string SUNIONSTORE = "SUNIONSTORE";

        public static readonly string SAVE = "SAVE";
        public static readonly string SET = "SET";
        public static readonly string SELECT = "SELECT";
        public static readonly string SETNAME = "SETNAME";
        public static readonly string SETNX = "SETNX";
        public static readonly string SHUTDOWN = "SHUTDOWN";

        public static readonly string TIME = "TIME";
        public static readonly string TTL = "TTL";
        public static readonly string TYPE = "TYPE";

        public static readonly string ZADD = "ZADD";
        public static readonly string ZCARD = "ZCARD";
        public static readonly string ZCOUNT = "ZCOUNT";
        public static readonly string ZINCRBY = "ZINCRBY";
        public static readonly string ZINTERSTORE = "ZINTERSTORE";
        public static readonly string ZRANGE = "ZRANGE";
        public static readonly string ZRANGEBYSCORE = "ZRANGEBYSCORE";
        public static readonly string ZRANK = "ZRANK";
        public static readonly string ZREM = "ZREM";
        public static readonly string ZREMRANGEBYRANK = "ZREMRANGEBYRANK";
        public static readonly string ZREMRANGEBYSCORE = "ZREMRANGEBYSCORE";
        public static readonly string ZREVRANGE = "ZREVRANGE";
        public static readonly string ZREVRANGEBYSCORE = "ZREVRANGEBYSCORE";
        public static readonly string ZREVRANK = "ZREVRANK";
        public static readonly string ZSCORE = "ZSCORE";
        public static readonly string ZUNIONSTORE = "ZUNIONSTORE";
    }
}
