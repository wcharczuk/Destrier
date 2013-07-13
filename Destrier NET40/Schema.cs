using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Destrier
{
    public class Schema
    {
        public static List<TableAttribute> GetTables(String connectionName, String databaseName = null)
        {
            List<TableAttribute> set = null; //new HashSet<String>();
            String query = null;
            if (!String.IsNullOrEmpty(databaseName))
            {
                query = String.Format("\nuse {0}", databaseName);
            }
            query = query + @"
select name from sys.tables where type_desc like 'USER_TABLE'
";

            Execute.StatementReader(query,
            (dr) =>
            {
                set = dr.ReadList<TableAttribute>();
            }, connectionName: connectionName);
            return set;
        }

        public static List<ColumnAttribute> GetColumnsForTable(String tableName, String databaseName = null, String connectionName = null)
        {
            List<ColumnAttribute> set = null; //new HashSet<String>();
            String query = null;
            if (!String.IsNullOrEmpty(databaseName))
            {
                query = String.Format("\nuse {0}", databaseName);
            }

            query = query + @"
select 
	sc.*
	, case when sc.name = k.column_name then 1 else 0 end as is_primarykey
from 
	sys.columns as sc 
	join sys.tables as st on sc.object_id = st.object_id
	left join information_schema.table_constraints as c on c.table_name = st.name and c.constraint_type = 'PRIMARY KEY'
	left join information_schema.key_column_usage as k on 
		c.table_name = k.table_name
		and sc.name = k.column_name
		and c.constraint_catalog = k.constraint_catalog
		and c.constraint_schema = k.constraint_schema
		and c.constraint_name = k.constraint_name
where  
    st.name = @tableName";

            Execute.StatementReader(query, 
            (dr) => {
                set = dr.ReadList<ColumnAttribute>();
            }, new { tableName = tableName }, connectionName: connectionName);
            return set;
        }
    }
}
