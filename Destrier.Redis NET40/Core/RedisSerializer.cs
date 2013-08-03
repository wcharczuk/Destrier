using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier.Redis.Core
{
    public static class RedisSerializer
    {
        public static IDictionary<String, Object> Serialize(Object obj, String keyPrefix = null)
        {
            if (obj == null)
                throw new ArgumentException("Cannot be null.", "obj");

            var map = ReflectionCache.GetMemberMap(obj.GetType());

            var dict = new Dictionary<String, Object>();

            foreach (var member in map)
            {
                if (!String.IsNullOrEmpty(keyPrefix))
                {
                    dict.Add(String.Format("{0}.{1}.{2}", keyPrefix, Model.GetKey(obj), member.FullyQualifiedName), member.GetValue(obj));
                }
                else
                {
                    dict.Add(String.Format("{0}.{1}", Model.GetKey(obj), member.FullyQualifiedName), member.GetValue(obj));
                }
            }

            return dict;
        }

        public static T Deserialize<T>(String key)
        {
            return default(T);
        }
    }
}
