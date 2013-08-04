using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
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
