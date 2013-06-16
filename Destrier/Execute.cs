using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Dynamic;
using System.Reflection;
using System.Collections;
using Microsoft.SqlServer.Server;
using System.Data;

namespace Destrier
{
    public static class Execute
    {
        public static void StoredProcedureReader(String storedProcedure, Action<IndexedSqlDataReader> action, dynamic procedureParams = null, String connectionString = null, Boolean standardizeCasing = true)
        {
            connectionString = connectionString ?? DatabaseConfigurationContext.DefaultConnectionString;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.CommandText = storedProcedure;

                    if (procedureParams != null)
                    {
                        Utility.AddParametersToCommand(procedureParams, cmd);
                    }

                    using (var dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        action(new IndexedSqlDataReader(dr, standardizeCasing));
                    }
                }
                conn.Close();
            }
        }

        public static void NonQuery(String statement, dynamic procedureParams = null, String connectionString = null)
        {
            connectionString = connectionString ?? DatabaseConfigurationContext.DefaultConnectionString;
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandText = statement;

                    if (procedureParams != null)
                    {
                        Utility.AddParametersToCommand(procedureParams, cmd);
                    }

                    cmd.ExecuteNonQuery();
                }
                conn.Close();
            }
        }

        public static void StatementReader(String statement, Action<IndexedSqlDataReader> action, dynamic procedureParams = null, String connectionString = null, Boolean standardizeCasing = true)
        {
            connectionString = connectionString ?? DatabaseConfigurationContext.DefaultConnectionString;
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandText = statement;

                    if (procedureParams != null)
                    {
                        Utility.AddParametersToCommand(procedureParams, cmd);
                    }

                    using (var dr = cmd.ExecuteReader())
                    {
                        action(new IndexedSqlDataReader(dr, standardizeCasing));
                    }
                }
                conn.Close();
            }
        }

        public static SqlCommand Command(String connectionString = null)
        {
            connectionString = connectionString ?? DatabaseConfigurationContext.DefaultConnectionString;

            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.Disposed += new EventHandler(cmd_Disposed);
            return cmd;
        }

        private static void cmd_Disposed(object sender, EventArgs e)
        {
            try
            {
                ((SqlCommand)sender).Connection.Close();
                ((SqlCommand)sender).Connection.Dispose();
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

            public static void AddWhereClauseVariables(dynamic procedureParams, StringBuilder commandText)
            {
                if (procedureParams == null)
                    return;

                if (!(procedureParams is IDictionary<String, Object>))
                    procedureParams = ((object)procedureParams).ToDynamic();

                foreach (KeyValuePair<string, object> member in (IDictionary<String, Object>)procedureParams)
                {
                    commandText.AppendLine(String.Format("and [{0}] = @{0}", member.Key));
                }
            }

            public static void AddParametersToCommand(dynamic procedureParams, SqlCommand cmd)
            {
                if (procedureParams == null)
                    return;

                if (!(procedureParams is IDictionary<String, Object>))
                    procedureParams = ((object)procedureParams).ToDynamic();

                foreach (KeyValuePair<String, Object> member in (IDictionary<String, Object>)procedureParams)
                {
                    object propertyValue = member.Value;
					//TODO: we need a better plan in place for handling list parameters.
					//this is a useful feature but implemented terribly in the mainline SqlClient (DataTables ftl.)
					//JSON to the rescue?
					//WC: Maybe pass the list as a 'json text parameter' and hope the database code knows what to do with it.
					//There is an example of a proc to consume JSON here: https://www.simple-talk.com/sql/t-sql-programming/consuming-json-strings-in-sql-server/
                    if (propertyValue is IList)
                    {
                        DataTable values = new DataTable();
                        values.Columns.Add(new DataColumn("value"));
                        foreach (object value in (IEnumerable)propertyValue)
                        {
                            if (value.GetType().IsEnum)
                                values.Rows.Add((int)value);
                            else
                                values.Rows.Add(value);
                        }

                        SqlParameter param = cmd.Parameters.AddWithValue(String.Format("@{0}", member.Key), values);
                        param.SqlDbType = System.Data.SqlDbType.Structured;
                    }
                    else
                        cmd.Parameters.AddWithValue(String.Format("@{0}", member.Key), propertyValue.DBNullCoalese());
                }
            }
        }
    }
}
