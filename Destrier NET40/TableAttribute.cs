using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier
{
    /// <summary>
    /// Class used to tell Destrier what SQL/Schema table to map the model to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
    public class TableAttribute : System.Attribute, IPopulate
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public TableAttribute() 
        { 
            this.UseNoLock = true; 
        }

        /// <summary>
        /// Constructor with TableName.
        /// </summary>
        /// <param name="tableName"></param>
        public TableAttribute(String tableName) : this()
        {
            this.TableName = tableName;
        }

        /// <summary>
        /// The name of the table to read/update/delete from.
        /// </summary>
        public String TableName { get; set; }

        /// <summary>
        /// The Database name of the table.
        /// </summary>
        public String DatabaseName { get; set; }

        /// <summary>
        /// The schema name of the table.
        /// </summary>
        /// <remarks>Defaults to "dbo"</remarks>
        public String SchemaName { get; set; }

        /// <summary>
        /// The connection string for the table. 
        /// </summary>
        public String ConnectionName { get; set; }

        /// <summary>
        /// Whether to apply the SQL query modifier "(NOLOCK)" when accessing the table.
        /// </summary>
        public Boolean UseNoLock { get; set; }

        /// <summary>
        /// Implementation of Populate.
        /// </summary>
        /// <param name="dr"></param>
        public void Populate(IndexedSqlDataReader dr)
        {
            this.TableName = dr.Get<String>("name");
        }
    }
}
