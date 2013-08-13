using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Destrier.Redis.Core
{
    public class Model
    {
        public static readonly String KeySeparator = ":";

        public static String CreateKey(params string[] keyComponents)
        {
            return String.Join(KeySeparator, keyComponents);
        }

        public static String GetKey(Object instance)
        {
            var type = instance.GetType();
            var members = ReflectionUtil.GenerateMemberMap(type, recursive: false);
            var keys = members.Where(m => m.IsKey).OrderBy(m => m.KeyOrder);
            var keyValues = keys.Select(m => m.GetValue(instance));

            var prefix = GetStoreKeyPrefix(type);

            if (!string.IsNullOrEmpty(prefix))
                return String.Join(KeySeparator, new String[] { prefix }.Concat(keyValues).ToArray());
            else
                return String.Join(KeySeparator, keyValues);
        }

        public static RedisKeyAttribute GetKeyAttribute(MemberInfo member)
        {
            return member.GetCustomAttribute(typeof(RedisKeyAttribute), false) as RedisKeyAttribute;
        }

        public static Boolean IsKey(MemberInfo member)
        {
            return GetKeyAttribute(member) != null;
        }

        public static Int32? GetKeyOrder(MemberInfo member)
        {
            var keyAttribute = GetKeyAttribute(member);
            if (keyAttribute != null)
                return keyAttribute.Order;

            return null;
        }

        public static RedisBinarySerializeAttribute GetBinarySerializeAttribute(MemberInfo member)
        {
            return member.GetCustomAttribute(typeof(RedisBinarySerializeAttribute), false) as RedisBinarySerializeAttribute;
        }

        public static Boolean IsBinarySerialized(MemberInfo member)
        {
            return GetBinarySerializeAttribute(member) != null;
        }

        public static RedisStoreAttribute GetRedisStoreAttribute(Type type)
        {
            return type.GetCustomAttribute(typeof(RedisStoreAttribute), false) as RedisStoreAttribute;
        }

        public static String GetStoreKeyPrefix(Type type)
        {
            var storeAttribute = GetRedisStoreAttribute(type);
            if (storeAttribute != null)
                return storeAttribute.KeyPrefix;

            return null;
        }

        public static Int32? GetStoreDb(Type type)
        {
            var storeAttribute = GetRedisStoreAttribute(type);
            if (storeAttribute != null)
                return storeAttribute.DB;

            return null;
        }

        public static String GetKeyForProperty<T, F>(Expression<Func<T, F>> expression)
        {
            return GetKeyForMemberExpression(expression.Body as MemberExpression);
        }
        
        public static String GetKeyForMemberExpression(MemberExpression memberExp)
        {
            if (memberExp == null)
                throw new ArgumentNullException("memberExp");

            List<String> visitedNames = new List<String>();

            Member member = null;

            if (memberExp.Member is PropertyInfo)
                member = new PropertyMember(memberExp.Member as PropertyInfo);
            else
                member = new FieldMember(memberExp.Member as FieldInfo);

            visitedNames.Add(member.Name);

            var visitedMemberExp = memberExp;
            while (visitedMemberExp.Expression.NodeType == ExpressionType.MemberAccess)
            {
                visitedMemberExp = memberExp.Expression as MemberExpression;
                if (visitedMemberExp.Member is PropertyInfo)
                {
                    var parent = new PropertyMember(visitedMemberExp.Member as PropertyInfo);
                    visitedNames.Add(parent.Name);
                }
                else if (visitedMemberExp.Member is FieldInfo)
                {
                    var parent = new FieldMember(visitedMemberExp.Member as FieldInfo);
                    visitedNames.Add(parent.Name);
                }
                else
                    return null; //abort!
            }

            visitedNames.Reverse();
            return String.Join(Model.KeySeparator, visitedNames);
        }

        public static long GetObjectSizeBytes(Object instance)
        {
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, instance);
                return ms.Length;
            }
        }
    }
}
