using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Destrier.Redis
{
    public static class RedisBinarySerializer
    {
        public static void Serialize(String key, Object instance, RedisClient rc = null)
        {
            var c = rc ?? RedisContext.GetClient();

            var bf = new BinaryFormatter();
            var ms = new MemoryStream();
            bf.Serialize(ms, instance);
            ms.Position = 0;
            c.SetBinary(key, ms);

            if (rc != null)
                c.Dispose();
        }

        public static T Deserialize<T>(String key, RedisClient rc = null)
        {
            var c = rc ?? RedisContext.GetClient();
            
            var bf = new BinaryFormatter();
            var buffer = c.GetBinary(key);
            var ms = new MemoryStream(buffer);
            var obj = bf.Deserialize(ms);

            if (rc != null)
                c.Dispose();

            return (T)obj;
        }
    }
}
