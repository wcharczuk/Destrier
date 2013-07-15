using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Reflection;
using System.Collections;
using System.Data;
using System.Data.Common;
using Destrier.Extensions;

namespace Destrier
{
    public static class Execute
    {
        public static void StoredProcedureReader(String storedProcedure, Action<IndexedSqlDataReader> action, dynamic parameters = null, String connectionName = null, Boolean standardizeCasing = true)
        {
            using (var cmd = Command(connectionName))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandText = storedProcedure;

                if (parameters != null)
                {
                    Utility.AddParametersToCommand(parameters, cmd);
                }

                using (var dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    action(new IndexedSqlDataReader(dr, standardizeCasing));
                }
            }
        }

        public static void NonQuery(String statement, dynamic procedureParams = null, String connectionName = null)
        {
            using (var cmd = Command(connectionName))
            {
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.CommandText = statement;

                if (procedureParams != null)
                {
                    Utility.AddParametersToCommand(procedureParams, cmd);
                }

                cmd.ExecuteNonQuery();
            }
        }

        public static void StatementReader(String statement, Action<IndexedSqlDataReader> action, dynamic parameters = null, String connectionName = null, Boolean standardizeCasing = true)
        {
            using(var cmd = Command(connectionName))
            {
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.CommandText = statement;

                if (parameters != null)
                {
                    Utility.AddParametersToCommand(parameters, cmd);
                }

                using (var dr = cmd.ExecuteReader())
                {
                    action(new IndexedSqlDataReader(dr, standardizeCasing));
                }
            }
        }

        public static System.Data.Common.DbCommand Command(String connectionName = null)
        {
            String connectionString = null; 
            DbProviderFactory provider = null;
            if (!String.IsNullOrEmpty(connectionName))
            {
                connectionString = DatabaseConfigurationContext.ConnectionStrings.ContainsKey(connectionName) ? DatabaseConfigurationContext.ConnectionStrings[connectionName] : DatabaseConfigurationContext.DefaultConnectionString;
                DatabaseConfigurationContext.DbProviders.TryGetValue(connectionName, out provider);
            }
            provider = provider ?? DatabaseConfigurationContext.DefaultProviderFactory;
            return Command(connectionString, provider);
        }

        public static System.Data.Common.DbCommand Command(String connectionString, DbProviderFactory providerFactory = null)
        {
            connectionString = connectionString ?? DatabaseConfigurationContext.DefaultConnectionString;
            providerFactory = providerFactory ?? DatabaseConfigurationContext.DefaultProviderFactory;

            var connection = providerFactory.CreateConnection();
            connection.ConnectionString = connectionString;
            connection.Open();
            var cmd = providerFactory.CreateCommand();
            cmd.Connection = connection;
            cmd.Disposed += new EventHandler(cmd_Disposed);
            return cmd;
        }

        private static void cmd_Disposed(object sender, EventArgs e)
        {
            try
            {
                ((DbCommand)sender).Connection.Close();
                ((DbCommand)sender).Connection.Dispose();
            }
            catch { }
        }

        public static class Utility
        {
            public static IDictionary<String, Object> DecomposeObject(object obj)
            {
                dynamic decomposed;

                if (obj == null)
                    return new ExpandoObject();

                if (!(obj is IDictionary<String, Object>))
                    decomposed = obj.ToDynamic();
                else
                    decomposed = obj;

                return decomposed as IDictionary<String, Object>;
            }

            public static void AddParameterToCommand(String name, Object value, DbCommand cmd, String connectionName = null, DbType? dbType = null)
            {
                DbProviderFactory provider = DatabaseConfigurationContext.GetProviderForConnection(connectionName);
                AddParameterToCommand(name, value, cmd, provider: provider);
            }

            public static void AddParameterToCommand(String name, Object value, DbCommand cmd, DbProviderFactory provider = null, DbType? dbType = null)
            {
                provider = provider ?? DatabaseConfigurationContext.DefaultProviderFactory;

                var dbParameter = provider.CreateParameter();
                dbParameter.ParameterName = String.Format("@{0}", name);
                dbParameter.Value = value.DBNullCoalese();

                if (dbType != null)
                    dbParameter.DbType = dbType.Value;

                cmd.Parameters.Add(dbParameter);
            }

            public static void AddParametersToCommand(dynamic procedureParams, DbCommand cmd, String connectionName = null)
            {
                DbProviderFactory provider = DatabaseConfigurationContext.GetProviderForConnection(connectionName);
                AddParametersToCommandFromProvider(procedureParams, cmd, provider);
            }

            public static void AddParametersToCommandFromProvider(dynamic procedureParams, DbCommand cmd, DbProviderFactory provider = null)
            {
                if (procedureParams == null)
                    return;

                provider = provider ?? DatabaseConfigurationContext.DefaultProviderFactory;

                if (!(procedureParams is IDictionary<String, Object>))
                    procedureParams = ((object)procedureParams).ToDynamic();

                foreach (KeyValuePair<String, Object> member in (IDictionary<String, Object>)procedureParams)
                {
                    object propertyValue = member.Value;
                    var dbParameter = provider.CreateParameter();
                    dbParameter.ParameterName = String.Format("@{0}", member.Key);
                    dbParameter.Value = propertyValue.DBNullCoalese();
                    cmd.Parameters.Add(dbParameter);
                }
            }
        }
    }
}