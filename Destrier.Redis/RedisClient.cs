using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Destrier.Redis.Core;
using cmd = Destrier.Redis.Core.RedisCommandLiteral;

namespace Destrier.Redis
{
    public partial class RedisClient : IDisposable
    {
        protected RedisConnection _connection = null;
        public virtual RedisConnection Connection { get { return _connection; } }

        public RedisClient(String host, int port = 6379, String password = null)
        {
            Host = host;
            Port = port;
            Password = password;
            _connection = RedisConnectionPool.GetConnection(host, port, password);
            _connection.Connect();
        }

        public RedisClient(RedisConnection connection)
        {
            _connection = connection;
            this.Host = connection.Host;
            this.Port = connection.Port;
            this.Password = connection.Password;
        }

        public String Host { get; set; }
        public Int32 Port { get; set; }
        public String Password { get; set; }

        public void Create(Object instance)
        {
            Serialize(instance);
        }

        public void Delete(Object instance)
        {
            var key = Model.GetKey(instance);
            var searchKey = String.Format("{0}{1}*", key, Model.KeySeparator);
            var keys = this.GetKeys(searchKey).ToArray();
            this.Remove(keys);
        }

        public void Update(Object instance)
        {
            Serialize(instance);
        }

        public void Update<T, F>(T instance, Expression<Func<T, F>> expression, F value)
        {
            if (value == null)
                throw new ArgumentException("Cannot be null.", "value");

            var prefix = Model.GetKey(instance);
            var memberKey = Model.GetKeyForMemberExpression(expression.Body as MemberExpression);
            var key = String.Format("{0}{1}{2}", prefix, Model.KeySeparator, memberKey);

            Set(key, RedisDataFormatUtil.FormatForStorage(value));
        }

        public T Get<T>(String key)
        {
            return Deserialize<T>(key);
        }

        public void Serialize(Object instance, String keyPrefix = null)
        {
            var type = instance.GetType();

            var instanceKey = Model.GetKey(instance);

            String fullPrefix = instanceKey;

            if (!String.IsNullOrEmpty(keyPrefix))
                String.Format("{0}{1}{2}", keyPrefix, Model.KeySeparator, instanceKey);

            var members = new Dictionary<Member, Object>();
            ReflectionUtil.MapToDictionary(instance, members);

            foreach(var member in members)
            {
                var fullKey = String.Format("{0}{1}{2}", fullPrefix, Model.KeySeparator, member.Key.FullyQualifiedName);

                var value = member.Value;

                if (member.Key.IsBinarySerialized)
                    BinarySerialize(fullKey, value);
                else
                    Set(fullKey, RedisDataFormatUtil.FormatForStorage(value));
            }
        }

        public T Deserialize<T>(String key)
        {
            return (T)Deserialize(typeof(T), key);
        }

        public Object Deserialize(Type type, String key)
        {
            var members = ReflectionUtil.GetMemberMap(type);
            var non_binary_keys = members.Where(m => !m.IsBinarySerialized).Select(kvp => String.Format("{0}{1}{2}", key, Model.KeySeparator, kvp.FullyQualifiedName)).ToList();

            var results = MultiGetInternal(non_binary_keys.ToArray()).ToList();

            var map = new Dictionary<String, RedisValue>();
            for (int x = 0; x < non_binary_keys.Count; x++)
                map.Add(non_binary_keys[x], results[x]);

            var instance = ReflectionUtil.GetNewObject(type);
            Populate(instance, map, key);

            return instance;
        }

        public void BinarySerialize(String key, Object instance)
        {
            var bf = new BinaryFormatter();
            var ms = new MemoryStream();
            bf.Serialize(ms, instance);
            ms.Position = 0;
            SetBinary(key, ms);
        }

        public object BinaryDeserialize(String key)
        {
            var bf = new BinaryFormatter();
            var buffer = GetBinary(key);
            var ms = new MemoryStream(buffer);
            var obj = bf.Deserialize(ms);

            return obj;
        }

        public T BinaryDeserialize<T>(String key)
        {
            return (T)BinaryDeserialize(key);
        }


        protected void Populate(object instance, Dictionary<String, RedisValue> values, String path = null)
        {
            var members = ReflectionUtil.GenerateMemberMap(instance.GetType(), recursive: false);

            foreach (var member in members)
            {
                String key =
                    !String.IsNullOrEmpty(path) ?
                        String.Format("{0}{1}{2}", path, Model.KeySeparator, member.Name) : member.Name;

                if (member.IsBinarySerialized)
                {
                    member.SetValue(instance, BinaryDeserialize(key));
                }
                else if (values.ContainsKey(key))
                {
                    var value = values[key];
                    member.SetValue(instance, Convert.ChangeType(value, member.MemberType));
                }
            }
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.OnConnectionReleased();
                _connection = null;
            }
        }
    }
}
