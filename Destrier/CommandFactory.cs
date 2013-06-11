using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Dynamic;

namespace Destrier
{
    public class CommandFactory
    {
        public static SqlCommand GetCommand(String connectionString = null)
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
    }
}
