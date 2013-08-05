using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Destrier.Redis.Core;

namespace Destrier.Redis.SessionState
{
    [Serializable]
    [RedisStore("RedisSessionState")]
    public class RedisSessionStateItem
    {
        [RedisKey(0)]
        public String ApplicationName { get; set; }

        [RedisKey(1)]
        public String SessionId { get; set; }
        
        public DateTime Created { get; set; }
        public DateTime Expires { get; set; }
        public DateTime LockDate { get; set; }
        public Int32 LockId { get; set; }
        public Int32 Timeout { get; set; }
        public Boolean Locked { get; set; }

        [RedisBinarySerialize]
        public Object SesssionItems { get; set; }

        public Int32 Flags { get; set; }
    }
}
