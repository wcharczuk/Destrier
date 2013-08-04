using System;
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

        public static readonly string CLIENT = "CLIENT";
        public static readonly string CONFIG = "CONFIG";

        public static readonly string DBSIZE = "DBSIZE";
        public static readonly string DECR = "DECR";
        public static readonly string DECRBY = "DECRBY";
        public static readonly string DEL = "DEL";

        public static readonly string ECHO = "ECHO";
        public static readonly string EXISTS = "EXISTS";
        public static readonly string EXPIRE = "EXPIRE";
        public static readonly string EXPIREAT = "EXPIREAT";

        public static readonly string FLUSHALL = "FLUSHALL";
        public static readonly string FLUSHDB = "FLUSHDB";
        
        public static readonly string GET = "GET";
        public static readonly string GETNAME = "GETNAME";

        public static readonly string INCR = "INCR";
        public static readonly string INCRBY = "INCRBY";

        public static readonly string KILL = "KILL";
        public static readonly string KEYS = "KEYS";

        public static readonly string LIST = "LIST";
        public static readonly string LASTSAVE = "LASTSAVE";

        public static readonly string MOVE = "MOVE";
        public static readonly string MGET = "MGET";
        public static readonly string MSET = "MSET";

        public static readonly string PERSIST = "PERSIST";
        public static readonly string PING = "PING";
        public static readonly string PTTL = "PTTL";

        public static readonly string QUIT = "QUIT";

        public static readonly string RANDOMKEY = "RANDOMKEY";
        public static readonly string RENAME = "RENAME";
        public static readonly string RENAMENX = "RENAMENX";
        public static readonly string REWRITE = "REWRITE";
        public static readonly string RESETSTAT = "RESETSTAT";

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

    }
}
