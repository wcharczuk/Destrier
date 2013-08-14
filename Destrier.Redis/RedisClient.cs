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

        public RedisClient(String host, int port = 6379, String password = null, Int32? db = null)
        {
            Host = host;
            Port = port;
            Password = password;
            Db = db;
            _connection = RedisConnectionPool.GetConnection(host, port, password);
            _connection.Connect();
            SelectDb();
        }

        public RedisClient(RedisHostInfo hostInfo)
        {
            this.Host = hostInfo.Host;
            this.Port = hostInfo.Port;
            this.Password = hostInfo.Password;
            _connection = RedisConnectionPool.GetConnection(this.Host, this.Port, this.Password);
            _connection.Connect();
            SelectDb();
        }

        protected void SelectDb()
        {
            var db = this.Db ?? 0;
            _connection.Send(cmd.SELECT, db);
            _connection.ReadForError();
        }

        public RedisClient(RedisConnection connection)
        {
            _connection = connection;
            this.Host = connection.Host;
            this.Port = connection.Port;
            this.Password = connection.Password;
        }

        public RedisHostInfo AsRedisHostInfo()
        {
            return new RedisHostInfo() { Host = this.Host, Port = this.Port, Password = this.Password, Db = this.Db };
        }

        public String Host { get; set; }
        public Int32 Port { get; set; }
        public String Password { get; set; }
        public Int32? Db { get; set; }

        public void RemoveSerializedObject(Object instance)
        {
            var key = Model.GetKey(instance);
            var searchKey = String.Format("{0}{1}*", key, Model.KeySeparator);
            var keys = this.GetKeys(searchKey).ToArray();

            if(keys != null && keys.Any())
                this.Remove(keys);
        }

        public void UpdateSerializedObjectValue<T, F>(T instance, Expression<Func<T, F>> expression, F value)
        {
            if (value == null)
                throw new ArgumentException("Cannot be null.", "value");

            var prefix = Model.GetKey(instance);
            var memberKey = Model.GetKeyForMemberExpression(expression.Body as MemberExpression);
            var key = Model.CreateKey(prefix, memberKey);

            Set(key, RedisDataFormatUtil.FormatForStorage(value));
        }

        public void SerializeObject(Object instance, String keyPrefix = null, TimeSpan? slidingExpiration = null)
        {
            var type = instance.GetType();

            var instanceKey = Model.GetKey(instance);
            var fullPrefix = instanceKey;

            if (!String.IsNullOrEmpty(keyPrefix))
                fullPrefix = Model.CreateKey(keyPrefix, instanceKey);

            var members = new Dictionary<Member, Object>();
            ReflectionUtil.MapToDictionary(instance, members);

            foreach(var member in members)
            {
                var fullKey = Model.CreateKey(fullPrefix, member.Key.FullyQualifiedName);

                var value = member.Value;

                if (member.Key.IsBinarySerialized)
                {
                    BinarySerializeObject(fullKey, value);
                    ExpireByTimespan(fullKey, slidingExpiration);
                }
                else
                {
                    if (value != null)
                    {
                        Set(fullKey, RedisDataFormatUtil.FormatForStorage(value));
                        ExpireByTimespan(fullKey, slidingExpiration);
                    }
                }
            }
        }

        public void ExpireByTimespan(String key, TimeSpan? slidingExpiration)
        {
            if (slidingExpiration != null)
            {
                if (slidingExpiration.Value.TotalMilliseconds >= 1000)
                    Expire(key, (long)slidingExpiration.Value.TotalSeconds);
                else if (slidingExpiration.Value.TotalMilliseconds > 0)
                    ExpireMilliseconds(key, (long)slidingExpiration.Value.TotalMilliseconds);
            }
        }

        public T DeserializeObject<T>(String key)
        {
            return (T)DeserializeObject(typeof(T), key);
        }

        public Object DeserializeObject(Type type, String key)
        {
            var members = ReflectionUtil.GetMemberMap(type);
            var non_binary_keys = members.Where(m => !m.IsBinarySerialized).Select(kvp => Model.CreateKey(key, kvp.FullyQualifiedName)).ToList();

            var results = MultiGetRawValues(non_binary_keys.ToArray()).ToList();

            var map = new Dictionary<String, RedisValue>();
            for (int x = 0; x < non_binary_keys.Count; x++)
                map.Add(non_binary_keys[x], results[x]);

            var instance = ReflectionUtil.GetNewObject(type);
            Populate(instance, map, key);

            return instance;
        }

        public long BinarySerializeObject(String key, Object instance)
        {
            var bf = new BinaryFormatter();
            var ms = new MemoryStream();
            bf.Serialize(ms, instance);
            ms.Position = 0;
            SetBinary(key, ms);

            return ms.Length;
        }

        public T BinaryDeserializeObject<T>(String key)
        {
            return (T)BinaryDeserializeObject(key);
        }

        public object BinaryDeserializeObject(String key)
        {
            var bf = new BinaryFormatter();
            var buffer = GetBinary(key);

            if (buffer == null)
                return null;

            var ms = new MemoryStream(buffer);
            var obj = bf.Deserialize(ms);

            return obj;
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
                    member.SetValue(instance, BinaryDeserializeObject(key));
                }
                else if (values.ContainsKey(key))
                {
                    object value = values[key];
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
