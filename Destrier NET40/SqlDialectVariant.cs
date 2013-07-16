using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier
{
    public interface ISqlDialectVariant
    {
        bool AltersOutputCasing { get; }

        string GenerateSelectLastId { get; }
        
        string NOLOCK { get; }
        string Like { get; } 
        string StringConcatOperator { get; }

        string GenerateSwitchDatabase(string databaseName);
        string WrapName(string name, Boolean isGeneratedAlias);
        string TempTablePrefix(string tableName);
    }

    public static class SqlDialectVariantFactory
    {
        public static ISqlDialectVariant GetSqlDialect(Type type)
        {
            var connectionName = Model.ConnectionName(type);
            return GetSqlDialect(connectionName);
        }

        public static ISqlDialectVariant GetSqlDialect(String connectionName)
        {
            var provider = DatabaseConfigurationContext.GetProviderForConnection(connectionName);

            if (provider is Npgsql.NpgsqlFactory)
                return ReflectionCache.GetNewObject<PostgresSqlDialectVariant>();
            else
                return ReflectionCache.GetNewObject<SqlServerSqlDialectVariant>();
        }
    }

    public class SqlServerSqlDialectVariant : ISqlDialectVariant
    {
        public bool AltersOutputCasing { get { return false; } }

        public string GenerateSelectLastId
        {
            get
            {
                return "SELECT @@IDENTITY";
            }
        }

        public string NOLOCK
        {
            get
            {
                return "(NOLOCK)";
            }
        }

        public String Like
        {
            get
            {
                return "Like";
            }
        }

        public String StringConcatOperator
        {
            get
            {
                return "+";
            }
        }

        public string GenerateSwitchDatabase(string databaseName)
        {
            return String.Format("USE {0};", databaseName);
        }

        public string WrapName(string name, Boolean isTableAlias)
        {
            return String.Format("[{0}]", name);
        }

        public string TempTablePrefix(string tableName)
        {
            return String.Format("#{0}", tableName);
        }
    }

    public class PostgresSqlDialectVariant : ISqlDialectVariant
    {
        public bool AltersOutputCasing
        {
            get { return true; }
        }

        public string GenerateSelectLastId
        {
            get { return "SELECT lastval();"; }
        }

        public string NOLOCK
        {
            get { return string.Empty; }
        }

        public string Like
        {
            get { return "ILIKE"; }
        }

        public string StringConcatOperator
        {
            get { return "||"; }
        }

        public string GenerateSwitchDatabase(string databaseName)
        {
            throw new NotImplementedException();
        }

        public string WrapName(string name, bool isGeneratedAlias)
        {
            if (isGeneratedAlias)
                return String.Format("\"{0}\"", name);
            else
                return name;
        }

        public string TempTablePrefix(string tableName)
        {
            return String.Format("__{0}", tableName);
        }
    }
}
