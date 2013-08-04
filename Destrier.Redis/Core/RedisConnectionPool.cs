using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Destrier.Redis.Core
{
    //general idea is to have a 'nominal' number of connections per server/port.
    //throw exception when we reach a max number.
    //when a new connection is requested, check if there is a 'free' connection already, and return that.
    //on startup, spin up a steady state (6?) connections for the host/port combo.
    public class RedisConnectionPool
    {
        static RedisConnectionPool()
        {
            _poolStore = new ConcurrentDictionary<String, Dictionary<Guid, RedisConnection>>();
            _poolStoreLocks = new ConcurrentDictionary<string, ReaderWriterLockSlim>();
        }

        ~RedisConnectionPool()
        {
            try
            {
                foreach(var hostPool in _poolStore)
                {
                    foreach(var conn in hostPool.Value.Values)
                    {
                        try
                        {
                            conn.Dispose();
                        }
                        catch{}
                    }
                }
            } catch{}
        }

        public const Int32 MAX_POOL_SIZE = 90;
        public const Int32 STEADY_POOL_SIZE = 6;

        private static ConcurrentDictionary<String, Dictionary<Guid, RedisConnection>> _poolStore;
        private static ConcurrentDictionary<String, ReaderWriterLockSlim> _poolStoreLocks;

        public static Int32 GetPoolSizeForHost(String host, int port)
        {
            var hostHash = _createConnectionHash(host, port);
            var hostLock = _getHostLock(host, port);

            hostLock.EnterReadLock();
            try
            {
                return _getPoolForHost(host, port).Count;
            }
            finally
            {
                hostLock.ExitReadLock();
            }
 
        }

        public static RedisConnection GetConnection(string host, int port, String password = null)
        {
            var hostHash = _createConnectionHash(host, port);
            var hostLock = _getHostLock(host, port);

            hostLock.EnterUpgradeableReadLock();
            try
            {
                var hostPool = _getPoolForHost(host, port);

                foreach (var conn in hostPool.Values)
                    if (!conn.IsAssignedToClient)
                        return conn;

                if (hostPool.Count >= MAX_POOL_SIZE)
                    throw new RedisException("RedisConnectionPool :: Maximum pool sized reached!");

                hostLock.EnterWriteLock();
                try
                {
                    var connection = _createConnection(host, port, password);
                    connection.IsAssignedToClient = true;
                    hostPool.Add(connection.Id, connection);

                    return connection;
                }
                finally
                {
                    hostLock.ExitWriteLock();
                }
            }
            finally
            {
                hostLock.ExitUpgradeableReadLock();
            }
        }

        private static RedisConnection _createConnection(String host, int port, String password = null)
        {
            var conn = new RedisConnection(host, port, password);

            conn.Disconnected += _connectionDisconnected;
            conn.ConnectionReleased += _connectionReleased;

            return conn;
        }

        /// <summary>
        /// Triggered when a client is done with a connection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void _connectionReleased(object sender, EventArgs e)
        {
            var conn = sender as RedisConnection;
            if (conn != null)
            {
                conn.IsAssignedToClient = false;
                var hostPool = _getPoolForHost(conn.Host, conn.Port);

                if (hostPool.Count > STEADY_POOL_SIZE)
                    _removeConnectionFromPool(conn);
                
            }
        }
        
        /// <summary>
        /// Triggered when a connection is forcebly closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void _connectionDisconnected(object sender, EventArgs e)
        {
            var conn = sender as RedisConnection;
            if (conn != null)
            {
                _removeConnectionFromPool(conn);
            }
        }

        private static void _removeConnectionFromPool(RedisConnection conn)
        {
            conn.IsAssignedToClient = false;

            var hostLock = _getHostLock(conn.Host, conn.Port);

            hostLock.EnterUpgradeableReadLock();
            try
            {
                var hostPool = _getPoolForHost(conn.Host, conn.Port);

                hostLock.EnterWriteLock();
                try { hostPool.Remove(conn.Id); }
                finally { hostLock.ExitWriteLock(); }
            }
            finally { hostLock.ExitUpgradeableReadLock(); }

            conn.Dispose();
            conn = null;
        }

        private static ReaderWriterLockSlim _getHostLock(String host, Int32 port)
        {
            var hostHash = _createConnectionHash(host, port);
            return _getHostLock(hostHash);
        }

        private static ReaderWriterLockSlim _getHostLock(String hostHash)
        {
            return _poolStoreLocks.GetOrAdd(hostHash, (hash) => { return new ReaderWriterLockSlim(); });
        }

        private static Dictionary<Guid, RedisConnection> _getPoolForHost(String host, Int32 port)
        {
            return _poolStore.GetOrAdd(_createConnectionHash(host,port), (hostHash) => new Dictionary<Guid, RedisConnection>());
        }

        private static string _createConnectionHash(string host, int port)
        {
            return string.Format("[{0}]:{1}", host, port);
        }
    }
}
