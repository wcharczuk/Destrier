using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Destrier.Redis.Core;
using Xunit;

namespace Destrier.Redis.Test
{
    public class RedisConnectionPool_Tests : RedisBaseTest
    {
        [Fact]
        public void ReUsePoolConnection_test()
        {
            var host = "127.0.0.1";
            var port = 6379;

            var connections = new List<RedisConnection>() 
            {
                RedisConnectionPool.GetConnection(host, port)
                , RedisConnectionPool.GetConnection(host, port)
                , RedisConnectionPool.GetConnection(host, port)
                , RedisConnectionPool.GetConnection(host, port)
                , RedisConnectionPool.GetConnection(host, port)
                , RedisConnectionPool.GetConnection(host, port)
            };

            foreach (var conn in connections)
            {
                conn.OnConnectionReleased();
            }

            Assert.True(RedisConnectionPool.GetPoolSizeForHost(host, port).Equals(6));

            var newConn = RedisConnectionPool.GetConnection(host, port);
            Assert.True(connections.Any(c => c.Id.Equals(newConn.Id)));
        }

        [Fact]
        public void SteadyStatePoolSize_test()
        {
            var host = "127.0.0.1";
            var port = 6379;

            var connections = new List<RedisConnection>() 
            {
                RedisConnectionPool.GetConnection(host, port)
                , RedisConnectionPool.GetConnection(host, port)
                , RedisConnectionPool.GetConnection(host, port)
                , RedisConnectionPool.GetConnection(host, port)
                , RedisConnectionPool.GetConnection(host, port)
                , RedisConnectionPool.GetConnection(host, port)
            };

            Assert.True(RedisConnectionPool.GetPoolSizeForHost(host, port).Equals(6));

            var incrementalConnections = new List<RedisConnection>() 
            {
                RedisConnectionPool.GetConnection(host, port)
                , RedisConnectionPool.GetConnection(host, port)
                , RedisConnectionPool.GetConnection(host, port)
            };

            Assert.True(RedisConnectionPool.GetPoolSizeForHost(host, port).Equals(9));

            foreach (var conn in incrementalConnections)
            {
                conn.OnConnectionReleased();
            }

            Assert.True(RedisConnectionPool.GetPoolSizeForHost(host, port).Equals(6));

            foreach (var conn in connections)
            {
                conn.OnConnectionReleased();
            }

            Assert.True(RedisConnectionPool.GetPoolSizeForHost(host, port).Equals(6));
        }

        [Fact]
        public void MaximumPoolSize_test()
        {
            var hostA = "127.0.0.1";
            var hostB = "192.168.1.0";
            var port = 6379;

            var a_did_throw_exception = false;
            var b_did_throw_exception = false;

            var a_connections = 0;
            var b_connections = 0;

            Action getConnection_A = () => {
                try { var conn = RedisConnectionPool.GetConnection(hostA, port); Interlocked.Increment(ref a_connections); }
                catch (RedisException) { a_did_throw_exception = true; }
            };

            Action getConnection_B = () => {
                try { var conn = RedisConnectionPool.GetConnection(hostB, port); Interlocked.Increment(ref b_connections); }
                catch(RedisException) { b_did_throw_exception = true; }
            };

            List<Task> tasks = new List<Task>();
            for (int x = 0; x < RedisConnectionPool.MAX_POOL_SIZE + 1; x++)
            {
                var t1 = Task.Factory.StartNew(getConnection_A);
                var t2 = Task.Factory.StartNew(getConnection_B);
                tasks.Add(t1); tasks.Add(t2);
            }
            Task.WaitAll(tasks.ToArray());

            Assert.True(a_did_throw_exception);
            Assert.True(b_did_throw_exception);
            Assert.True(a_connections == RedisConnectionPool.MAX_POOL_SIZE);
            Assert.True(b_connections == RedisConnectionPool.MAX_POOL_SIZE);
        }
    }
}
