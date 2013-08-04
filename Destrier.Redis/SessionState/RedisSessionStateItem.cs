using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Destrier.Redis.SessionState
{
    public class RedisSessionStateItem
    {
        public String SessionId { get; set; }
        public String ApplicationName { get; set; }
        public DateTime Created { get; set; }
        public DateTime Expires { get; set; }
        public DateTime LockDate { get; set; }
        public Int32 LockId { get; set; }
        public Int32 Timeout { get; set; }
        public Boolean Locked { get; set; }
        public Object SesssionItems { get; set; }
        public Int32 Flags { get; set; }
    }
}
