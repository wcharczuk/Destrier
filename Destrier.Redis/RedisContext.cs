using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Destrier.Redis
{
    public class RedisContext
    {
        static RedisContext()
        {
            DefaultPort = 6379;
        }

        public static String DefaultHost { get; set; }
        public static Int32 DefaultPort { get; set; }
        public static String DefaultPassword { get; set; }

        public static RedisClient GetClient()
        {
            return new RedisClient(DefaultHost, DefaultPort, DefaultPassword);
        }
    }
}
