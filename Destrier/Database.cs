using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Reflection;
using System.Linq.Expressions;

namespace Destrier
{
    public class Database
    {
        public static void Create(BaseModel myObject)
        {
            Type myObjectType = myObject.GetType();

            myObject.OnPreCreate(new EventArgs());
            
            if (ReflectionCache.HasInterface(myObjectType, typeof(IAuditable)))
            {
                ((IAuditable)myObject).DoAudit(DatabaseAction.Create, myObject);
            }

            StringBuilder command = new StringBuilder();
            var connectionString = Model.ConnectionString(myObjectType);
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandType = System.Data.CommandType.Text;

                    command.Append("INSERT INTO " + Model.TableNameFullyQualified(myObjectType) + " (");

                    List<String> columnNames = new List<String>();
                    foreach (PropertyInfo pi in Model.Columns(myObjectType))
                    {
                        ColumnAttribute column = pi.GetCustomAttributes(true).FirstOrDefault(ca => ca is ColumnAttribute) as ColumnAttribute;
                        String columnName = String.IsNullOrEmpty(column.Name) ? pi.Name.ToLowerCaseFirstLetter() : column.Name;
                        if (!column.IsForReadOnly && !(Model.HasAutoIncrementColumn(myObjectType) && column.IsPrimaryKey))
                        {
                            columnNames.Add(columnName);
                        }
                    }

                    command.Append(string.Join(", ", columnNames.Select(columnName => String.Format("[{0}]", columnName))));
                    command.Append(") VALUES (");

                    command.Append(string.Join(", ", columnNames.Select(s => "@" + s)));
                    command.Append(");");

                    foreach (PropertyInfo pi in Model.Columns(myObjectType))
                    {
                        ColumnAttribute column = pi.GetCustomAttributes(true).FirstOrDefault(ca => ca is ColumnAttribute) as ColumnAttribute;

                        if (!column.IsForReadOnly && !(Model.HasAutoIncrementColumn(myObjectType) && column.IsPrimaryKey))
                        {
                            AddColumnParameter(pi, column, myObject, cmd);
                        }
                    }

                    if (Model.HasAutoIncrementColumn(myObjectType))
                    {
                        command.Append("SELECT @@IDENTITY;");
                        cmd.CommandText = command.ToString();

                        object o = cmd.ExecuteScalar();
                        if (o != null && !(o is DBNull))
                        {
                            Model.AutoIncrementColumn(myObjectType).SetValue(myObject, Convert.ChangeType(o, Model.AutoIncrementColumn(myObjectType).PropertyType), null);
                        }
                    }
                    else
                    {
                        cmd.CommandText = command.ToString();
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            myObject.OnPostCreate(new EventArgs());
        }

        public static void Update(BaseModel myObject)
        {
            Type myObjectType = myObject.GetType();

            if (ReflectionCache.HasInterface(myObjectType, typeof(IAuditable)))
            {
                var previous = ((IAuditable)myObject).AuditGetPrevious(myObject);
                ((IAuditable)myObject).DoAudit(DatabaseAction.Update, myObject, previous);
            }

            myObject.OnPreUpdate(new EventArgs());

            StringBuilder command = new StringBuilder();
            var connectionString = Model.ConnectionString(myObjectType);
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandType = System.Data.CommandType.Text;

                    command.Append("UPDATE ");
                    command.Append(Model.TableNameFullyQualified(myObjectType));
                    command.Append(" SET ");
                    //set
                    List<String> variables = new List<String>();
                    foreach (PropertyInfo pi in Model.ColumnsNonPrimaryKey(myObjectType))
                    {
                        ColumnAttribute column = pi.GetCustomAttributes(true).FirstOrDefault(ca => ca is ColumnAttribute) as ColumnAttribute;
                        if (!column.IsForReadOnly)
                        {
                            if (!String.IsNullOrEmpty(column.Name))
                                variables.Add(column.Name);
                            else
                                variables.Add(pi.Name.ToLowerCaseFirstLetter());
                        }
                    }

                    command.Append(string.Join(", ", variables.Select(variableName => String.Format("[{0}] = @{0}", variableName))));

                    command.Append(" WHERE ");

                    variables = new List<String>();
                    foreach (PropertyInfo pi in Model.ColumnsPrimaryKey(myObjectType))
                    {
                        ColumnAttribute column = pi.GetCustomAttributes(true).FirstOrDefault(ca => ca is ColumnAttribute) as ColumnAttribute;
                        if (!String.IsNullOrEmpty(column.Name))
                            variables.Add(column.Name);
                        else
                            variables.Add(pi.Name.ToLowerCaseFirstLetter());
                    }
                    command.Append(string.Join(" and ", variables.Select(variableName => String.Format("[{0}] = @{0}", variableName))));

                    //parameters
                    foreach (PropertyInfo pi in Model.Columns(myObjectType))
                    {
                        ColumnAttribute column = pi.GetCustomAttributes(true).FirstOrDefault(ca => ca is ColumnAttribute) as ColumnAttribute;

                        if (!column.IsForReadOnly)
                        {
                            AddColumnParameter(pi, column, myObject, cmd);
                        }
                    }

                    cmd.CommandText = command.ToString();
                    cmd.ExecuteNonQuery();
                }
            }

            myObject.OnPostUpdate(new EventArgs());
        }

        public static void Remove(BaseModel myObject)
        {
            Type myObjectType = myObject.GetType();

            myObject.OnPreRemove(new EventArgs());

            if (ReflectionCache.HasInterface(myObjectType, typeof(IAuditable)))
            {
                ((IAuditable)myObject).DoAudit(DatabaseAction.Delete, myObject);
            }

            StringBuilder command = new StringBuilder();
            var connectionString = Model.ConnectionString(myObjectType);
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandType = System.Data.CommandType.Text;
                    command.Append("DELETE FROM " + Model.TableNameFullyQualified(myObjectType) + " WHERE ");

                    List<String> variables = new List<String>();
                    foreach (PropertyInfo pi in Model.ColumnsPrimaryKey(myObjectType))
                    {
                        ColumnAttribute column = pi.GetCustomAttributes(true).FirstOrDefault(ca => ca is ColumnAttribute) as ColumnAttribute;
                        if (!String.IsNullOrEmpty(column.Name))
                            variables.Add(column.Name);
                        else
                            variables.Add(pi.Name.ToLowerCaseFirstLetter());
                    }
                    command.Append(string.Join(" and ", variables.Select(variableName => String.Format("[{0}] = @{0}", variableName))));

                    foreach (PropertyInfo pi in Model.ColumnsPrimaryKey(myObjectType))
                    {
                        ColumnAttribute column = pi.GetCustomAttributes(true).FirstOrDefault(ca => ca is ColumnAttribute) as ColumnAttribute;
                        string columnName = String.IsNullOrEmpty(column.Name) ? pi.Name : column.Name;
                        cmd.Parameters.AddWithValue("@" + columnName, pi.GetValue(myObject, null));
                    }

                    cmd.CommandText = command.ToString();
                    cmd.ExecuteNonQuery();
                }
            }

            myObject.OnPostRemove(new EventArgs());
        }

        public static void RemoveWhere<T>(Expression<Func<T, bool>> expression = null)
        {
            Type myObjectType = typeof(T);
            StringBuilder command = new StringBuilder();
            var connectionString = Model.ConnectionString(myObjectType);
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandType = System.Data.CommandType.Text;
                    command.Append("DELETE FROM ");
                    command.Append(Model.TableNameFullyQualified(typeof(T)));

                    if (expression != null)
                    {
                        command.Append(" WHERE \n");
                        var parameters = new Dictionary<String, object>();
                        var visitor = new SqlExpressionVisitor<T>(command, parameters);
                        var body = expression.Body;
                        visitor.Visit(body);
                        Execute.Utility.AddParametersToCommand(parameters, cmd);
                    }

                    cmd.CommandText = command.ToString();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static T Get<T>(dynamic parameters = null) where T : BaseModel
        {
            if (ReflectionCache.HasInterface(typeof(T), typeof(IGet<T>)))
            {
                return ((IGet<T>)ReflectionCache.GetNewObject<T>()).Get(parameters) as T;
            }

            if (parameters == null)
                throw new ArgumentNullException("parameters");

            var obj = new Query<T>().Where(parameters).Limit(1).Execute();

            return System.Linq.Enumerable.FirstOrDefault(obj);
        }

        public static IEnumerable<T> All<T>() where T : BaseModel
        {
            if (ReflectionCache.HasInterface(typeof(T), typeof(IGetMany<T>)))
            {
                return ((IGetMany<T>)ReflectionCache.GetNewObject<T>()).GetMany() as IEnumerable<T>;
            }

            return new Query<T>().Execute();
        }

        #region Utility
        private static void AddColumnParameter(PropertyInfo property, ColumnAttribute column, object myObject, SqlCommand cmd)
        {
            object value = property.GetValue(myObject, null).DBNullCoalese();
            String columnName = String.IsNullOrEmpty(column.Name) ? property.Name.ToLowerCaseFirstLetter() : column.Name;

            if (!Model.CheckNullStateForColumn(column, value))
                throw new InvalidColumnDataException(column, value);

            if (!Model.CheckLengthForColumn(column, value))
                if (column.MaxStringLength != default(Int32) && column.ShouldTrimLongStrings && value != null)
                    value = value.ToString().Substring(0, column.MaxStringLength - 1);
                else
                    throw new InvalidColumnDataException(column, value);

            if ((int)column.SqlDbType == (-1))
                cmd.Parameters.AddWithValue("@" + columnName, value);
            else
                cmd.Parameters.Add(new SqlParameter() { SqlDbType = column.SqlDbType, ParameterName = "@" + columnName, Value = value });
        }
        #endregion
    }
}