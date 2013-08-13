using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Destrier.Redis
{
    public class RedisContext
    {
        public static String DefaultHostName { get; set; }

        public static RedisHostInfo DefaultHost
        {
            get
            {
                if (_hostInfos.Any() && _hostInfos.Count == 1)
                    return _hostInfos.Values.First();
                else if (!String.IsNullOrEmpty(DefaultHostName))
                    return _hostInfos[DefaultHostName];
                else
                    return null;
            }
        }

        public static ConcurrentDictionary<String, RedisHostInfo> _hostInfos = new ConcurrentDictionary<string, RedisHostInfo>();

        public static void AddHost(String name, String hostName, Int32 port = 6379, String password = null, Int32? db = null)
        {
            _hostInfos.TryAdd(name, new RedisHostInfo() { Host = hostName, Port = port, Password = password, Db = db });
        }

        public static RedisClient GetClient()
        {
            if (DefaultHost != null)
                return new RedisClient(DefaultHost);
            else
                return null;
        }

        public static RedisClient GetClient(String hostName)
        {
            if (_hostInfos.ContainsKey(hostName))
                return new RedisClient(_hostInfos[hostName]);
            else
                return null;
        }
    }
}
