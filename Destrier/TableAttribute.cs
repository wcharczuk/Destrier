using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
    public class TableAttribute : System.Attribute, IPopulate
    {
        public TableAttribute() { this.SchemaName = "dbo"; }
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
        public String ConnectionStringName { get; set; }

        public void Populate(IndexedSqlDataReader dr)
        {
            this.TableName = dr.Get<String>("name");
        }
    }
}
