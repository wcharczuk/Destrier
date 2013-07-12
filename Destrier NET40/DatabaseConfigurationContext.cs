using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading;

namespace Destrier
{
    /// <summary>
    /// This is main class for setting up the connection strings associated with the application database context.
    /// </summary>
    public class DatabaseConfigurationContext
    {
        private static Dictionary<String, String> _connectionStrings = null;
        private static object _connectionLock = new object();
        /// <summary>
        /// A mapping between connection 'name's (think, short hand names for connections) and their connection strings.
        /// </summary>
        public static Dictionary<String, String> ConnectionStrings
        {
            get
            {
                Monitor.Enter(_connectionLock);
                try
                {
                    if (null == _connectionStrings)
                    {
                        _connectionStrings = new Dictionary<string, string>();
                    }
                    return _connectionStrings;
                }
                finally
                {
                    Monitor.Exit(_connectionLock);
                }
            }
        }

        /// <summary>
        /// The default database name.
        /// </summary>
        public static String DefaultDatabaseName { get; set; }

        /// <summary>
        /// The default schema name.
        /// </summary>
        public static String DefaultSchemaName { get; set; }

        /// <summary>
        /// This is the default connection 'name' to use when there is no connection name specified.
        /// </summary>
        public static String DefaultConnectionName { get; set; }

        /// <summary>
        /// The corresponding connection string associated with the DefaultConnectionName
        /// </summary>
        public static String DefaultConnectionString
        {
            get
            {
                if (!String.IsNullOrEmpty(DefaultConnectionName))
                    return ConnectionStrings[DefaultConnectionName];
                else if (ConnectionStrings.Count == 1)
                    return ConnectionStrings.First().Value;
                else
                    return String.Empty;
            }
        }

        /// <summary>
        /// Reads Connection Strings from Web.config or App.config.
        /// </summary>
        public static void ReadFromConfiguration()
        {
            if (ConfigurationManager.ConnectionStrings != null && ConfigurationManager.ConnectionStrings.Count > 0)
            {
                for(int index = 0; index < ConfigurationManager.ConnectionStrings.Count; index++)
                {
                    var connString = ConfigurationManager.ConnectionStrings[index];
                    ConnectionStrings.Add(connString.Name, connString.ConnectionString);
                }
            }
        }
    }
}
