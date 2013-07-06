using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Destrier
{
    public class Schema
    {
        public static List<TableAttribute> GetTables(String connectionString, String databaseName = null)
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
            }, connectionString: connectionString);
            return set;
        }

        public static List<ColumnAttribute> GetColumnsForTable(String tableName, String databaseName = null, String connectionString = null)
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
            }, new { tableName = tableName }, connectionString: connectionString);
            return set;
        }

        public enum SqlSysType
        {
            IMAGE = 34,
            TEXT = 35,
            UNIQUEIDENTIFIER = 36,
            DATE = 40,
            TIME = 41,
            DATETIME2 = 42,
            DATETIMEOFFSET = 43,
            TINYINT = 48,
            SMALLINT = 52,
            INT = 56,
            SMALLDATETIME = 58,
            REAL = 59,
            MONEY = 60,
            DATETIME = 61,
            FLOAT = 62,
            SQL_VARIANT = 98,
            NTEXT = 99,
            BIT = 104,
            DECIMAL = 106,
            NUMERIC = 108,
            SMALLMONEY = 122,
            BIGINT = 127,
            HIERARCHYID = 240,
            GEOMETRY = 240,
            GEOGRAPHY = 240,
            VARBINARY = 165,
            VARCHAR = 167,
            BINARY = 173,
            CHAR = 175,
            TIMESTAMP = 189,
            NVARCHAR = 231,
            NCHAR = 239,
            XML = 241,
            SYSNAME = 231
        }

        public static Type GetClrType(SqlSysType system_type_id, Boolean isNullable = false)
        {
            if (isNullable)
            {
                switch (system_type_id)
                {
                    case SqlSysType.BIGINT:
                        return typeof(long?);

                    case SqlSysType.BINARY:
                    case SqlSysType.IMAGE:
                    case SqlSysType.TIMESTAMP:
                    case SqlSysType.VARBINARY:
                        return typeof(byte[]);

                    case SqlSysType.BIT:
                        return typeof(bool?);

                    case SqlSysType.CHAR:
                    case SqlSysType.NCHAR:
                    case SqlSysType.NTEXT:
                    case SqlSysType.NVARCHAR:
                    case SqlSysType.TEXT:
                    case SqlSysType.VARCHAR:
                    case SqlSysType.XML:
                        return typeof(string);

                    case SqlSysType.DATETIME:
                    case SqlSysType.SMALLDATETIME:
                    case SqlSysType.DATE:
                    case SqlSysType.TIME:
                    case SqlSysType.DATETIME2:
                        return typeof(DateTime?);

                    case SqlSysType.DECIMAL:
                    case SqlSysType.MONEY:
                    case SqlSysType.SMALLMONEY:
                        return typeof(decimal?);

                    case SqlSysType.FLOAT:
                        return typeof(double?);

                    case SqlSysType.INT:
                        return typeof(int?);

                    case SqlSysType.REAL:
                        return typeof(float?);

                    case SqlSysType.UNIQUEIDENTIFIER:
                        return typeof(Guid?);

                    case SqlSysType.SMALLINT:
                        return typeof(short?);

                    case SqlSysType.TINYINT:
                        return typeof(byte?);

                    case SqlSysType.DATETIMEOFFSET:
                        return typeof(DateTimeOffset?);

                    default:
                        throw new ArgumentOutOfRangeException("sqlType");
                }
            }
            else
            {
                switch (system_type_id)
                {
                    case SqlSysType.BIGINT:
                        return typeof(long);

                    case SqlSysType.BINARY:
                    case SqlSysType.IMAGE:
                    case SqlSysType.TIMESTAMP:
                    case SqlSysType.VARBINARY:
                        return typeof(byte[]);

                    case SqlSysType.BIT:
                        return typeof(bool);

                    case SqlSysType.CHAR:
                    case SqlSysType.NCHAR:
                    case SqlSysType.NTEXT:
                    case SqlSysType.NVARCHAR:
                    case SqlSysType.TEXT:
                    case SqlSysType.VARCHAR:
                    case SqlSysType.XML:
                        return typeof(string);

                    case SqlSysType.DATETIME:
                    case SqlSysType.SMALLDATETIME:
                    case SqlSysType.DATE:
                    case SqlSysType.TIME:
                    case SqlSysType.DATETIME2:
                        return typeof(DateTime);

                    case SqlSysType.DECIMAL:
                    case SqlSysType.MONEY:
                    case SqlSysType.SMALLMONEY:
                        return typeof(decimal);

                    case SqlSysType.FLOAT:
                        return typeof(double);

                    case SqlSysType.INT:
                        return typeof(int);

                    case SqlSysType.REAL:
                        return typeof(float);

                    case SqlSysType.UNIQUEIDENTIFIER:
                        return typeof(Guid);

                    case SqlSysType.SMALLINT:
                        return typeof(short);

                    case SqlSysType.TINYINT:
                        return typeof(byte);

                    case SqlSysType.DATETIMEOFFSET:
                        return typeof(DateTimeOffset);

                    default:
                        throw new ArgumentOutOfRangeException("sqlType");
                }
            }
        }
    }
}
