using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Destrier.Redis.Core;

namespace Destrier.Redis
{
    public class Redis
    {
        public static void Create(Object instance)
        {
            using (var rc = RedisContext.GetClient())
            {
                rc.Serialize(instance);
            }
        }

        public static void Delete(Object instance)
        {
            using (var rc = RedisContext.GetClient())
            {
                var key = Model.GetKey(instance);
                var searchKey = String.Format("{0}{1}*", key, Model.KeySeparator);
                var keys = rc.GetKeys(searchKey);
                rc.Remove(keys);
            }
        }

        public static void Update(Object instance)
        {
            using (var rc = RedisContext.GetClient())
            {
                rc.Serialize(instance);
            }
        }

        public static void Set<T, F>(T instance, Expression<Func<T, F>> expression, F value)
        {
            if (value == null)
                throw new ArgumentException("Cannot be null.", "value");

            using (var rc = RedisContext.GetClient())
            {
                var prefix = Model.GetStoreKeyPrefix(typeof(T));
                var memberKey = Model.GetKeyForMemberExpression(expression.Body as MemberExpression);
                var key = String.Format("{0}{1}{2}", prefix, Model.KeySeparator, memberKey);

                rc.Set(key, value.ToString());
            }
        }

        public static T Get<T>(String key)
        {
            using (var rc = RedisContext.GetClient())
            {
                return rc.Deserialize<T>(key);
            }
        }
    }
}
