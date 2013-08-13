using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Destrier.Redis
{
    public class RedisHostInfo
    {
        public RedisHostInfo()
        {
            Port = 6379;
        }

        public String Host { get; set; }
        public Int32 Port { get; set; }
        public String Password { get; set; }
        public Int32? Db { get; set; }
    }
}
