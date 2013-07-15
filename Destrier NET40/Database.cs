using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Reflection;
using System.Linq.Expressions;
using Destrier.Extensions;
using System.Data.Common;

namespace Destrier
{
    public class Database
    {
        /// <summary>
        /// Create an instance of <paramref name="myObject"/> in the database.
        /// </summary>
        /// <param name="myObject">Object to create</param>
        /// <remarks>Will fill the objects primary key property if it is set to auto increment.</remarks>
        public static void Create(object myObject)
        {
            Type myObjectType = myObject.GetType();

			if(ReflectionCache.HasInterface(myObjectType, typeof(IPreCreate)))
            	((IPreCreate)myObject).PreCreate();

            if (ReflectionCache.HasInterface(myObjectType, typeof(IAuditable)))
                ((IAuditable)myObject).DoAudit(DatabaseAction.Create, myObject);

            StringBuilder command = new StringBuilder();
            var connectionName = Model.ConnectionName(myObjectType);
            var sqlDialectider = SqlDialectVariantFactory.GetSqlDialect(myObjectType);
            using (var cmd = Execute.Command(connectionName))
            {
                cmd.CommandType = System.Data.CommandType.Text;

                command.Append("INSERT INTO " + Model.TableNameFullyQualified(myObjectType) + " (");

                List<String> columnNames = new List<String>();
                foreach (var cm in ReflectionCache.GetColumnMembers(myObjectType))
                {
                    if (!cm.ColumnAttribute.IsForReadOnly && !(Model.HasAutoIncrementColumn(myObjectType) && cm.IsPrimaryKey))
                    {
                        columnNames.Add(cm.Name);
                    }
                }

                command.Append(string.Join(", ", columnNames.Select(columnName => String.Format("{0}", sqlDialectider.WrapName(columnName, isGeneratedAlias:false)))));
                command.Append(") VALUES (");

                command.Append(string.Join(", ", columnNames.Select(s => "@" + s)));
                command.Append(");");

                foreach (var cm in ReflectionCache.GetColumnMembers(myObjectType))
                {
                    if (!cm.ColumnAttribute.IsForReadOnly && !(Model.HasAutoIncrementColumn(myObjectType) && cm.IsPrimaryKey))
                    {
                        AddColumnParameter(cm, myObject, cmd, connectionName);
                    }
                }

                if (Model.HasAutoIncrementColumn(myObjectType))
                {
                    
                    command.Append(sqlDialectider.GenerateSelectLastId);
                    cmd.CommandText = command.ToString();

                    object o = cmd.ExecuteScalar();
                    if (o != null && !(o is DBNull))
                    {
                        var aicm = Model.AutoIncrementColumn(myObjectType);
                        aicm.SetValue(myObject, Convert.ChangeType(o, aicm.Property.PropertyType));
                    }
                }
                else
                {
                    cmd.CommandText = command.ToString();
                    cmd.ExecuteNonQuery();
                }
            }

			if(ReflectionCache.HasInterface(myObjectType, typeof(IPostCreate)))
				((IPostCreate)myObject).PostCreate();
        }

        /// <summary>
        /// Create the instance if the where clause returns an empty set.
        /// </summary>
        /// <typeparam name="T">The model type.</typeparam>
        /// <param name="instance">An instance of the model type.</param>
        /// <param name="whereClause">The where clause to test.</param>
        /// <remarks>Calls Create() on the instance.</remarks>
        public static void CreateIfNotExists<T>(T instance, Expression<Func<T, bool>> whereClause) where T : new()
        {
            var query = new Query<T>().Where(whereClause);
            if (!query.StreamResults().Any())
            {
                Create(instance);
            }
        }

        /// <summary>
        /// Update the <paramref name="myObject"/>.
        /// </summary>
        /// <param name="myObject"></param>
        /// <remarks>Updates every property marked as a column.</remarks>
        public static void Update(object myObject)
        {
            Type myObjectType = myObject.GetType();

            if (ReflectionCache.HasInterface(myObjectType, typeof(IAuditable)))
            {
                var previous = ((IAuditable)myObject).AuditGetPrevious(myObject);
                ((IAuditable)myObject).DoAudit(DatabaseAction.Update, myObject, previous);
            }

			if(ReflectionCache.HasInterface(myObjectType, typeof(IPreUpdate)))
				((IPreUpdate)myObject).PreUpdate();

            StringBuilder command = new StringBuilder();
            var connectionName = Model.ConnectionName(myObjectType);
            var sqlDialectider = SqlDialectVariantFactory.GetSqlDialect(myObjectType);
            using (var cmd = Execute.Command(connectionName))
            {
                cmd.CommandType = System.Data.CommandType.Text;

                command.Append("UPDATE ");
                command.Append(Model.TableNameFullyQualified(myObjectType));
                command.Append(" SET ");
                

                List<String> variables = new List<String>();

                foreach (var cm in Model.ColumnsNonPrimaryKey(myObjectType))
                    if (!cm.ColumnAttribute.IsForReadOnly)
                        variables.Add(cm.Name);

                command.Append(string.Join(", ", variables.Select(variableName => String.Format("{0} = @{1}", sqlDialectider.WrapName(variableName, isGeneratedAlias: false), variableName))));

                command.Append(" WHERE ");

                variables = new List<String>();
                
                foreach (var cm in Model.ColumnsPrimaryKey(myObjectType))
                    variables.Add(cm.Name);

                command.Append(string.Join(" and ", variables.Select(variableName => String.Format("{0} = @{1}", sqlDialectider.WrapName(variableName, isGeneratedAlias: false), variableName))));

                //parameters
                foreach (var cm in ReflectionCache.GetColumnMembers(myObjectType))
                {
                    if (!cm.IsForReadOnly)
                    {
                        AddColumnParameter(cm, myObject, cmd, connectionName);
                    }
                }

                cmd.CommandText = command.ToString();
                cmd.ExecuteNonQuery();
            }
            
			if(ReflectionCache.HasInterface(myObjectType, typeof(IPostUpdate)))
				((IPostUpdate)myObject).PostUpdate();
        }

        /// <summary>
        /// Remove the <paramref name="myObject"/> from the database.
        /// </summary>
        /// <param name="myObject">The instance to remove.</param>
        public static void Remove(object myObject)
        {
            Type myObjectType = myObject.GetType();

			if(ReflectionCache.HasInterface(myObjectType, typeof(IPreRemove)))
				((IPreRemove)myObject).PreRemove();

            if (ReflectionCache.HasInterface(myObjectType, typeof(IAuditable)))
                ((IAuditable)myObject).DoAudit(DatabaseAction.Delete, myObject);
            
            StringBuilder command = new StringBuilder();
            var connectionName = Model.ConnectionName(myObjectType);
            var sqlDialectider = SqlDialectVariantFactory.GetSqlDialect(myObjectType);
            using (var cmd = Execute.Command(connectionName))
            {
                cmd.CommandType = System.Data.CommandType.Text;
                command.Append("DELETE FROM " + Model.TableNameFullyQualified(myObjectType) + " WHERE ");

                List<String> variables = new List<String>();
                
                foreach (var cm in Model.ColumnsPrimaryKey(myObjectType))
                    variables.Add(cm.Name);

                command.Append(string.Join(" and ", variables.Select(variableName => String.Format("{0} = @{1}", sqlDialectider.WrapName(variableName, isGeneratedAlias: false), variableName))));

                foreach (var cm in Model.ColumnsPrimaryKey(myObjectType))
                {
                    Execute.Utility.AddParameterToCommand(cm.Name, cm.Property.GetValue(myObject, null), cmd, connectionName);
                }

                cmd.CommandText = command.ToString();
                cmd.ExecuteNonQuery();
            }
            
			if(ReflectionCache.HasInterface(myObjectType, typeof(IPostRemove)))
				((IPostRemove)myObject).PostRemove();
        }

        /// <summary>
        /// Remove all the instances of <typeparamref name="T"/> that satisfy the <paramref name="expression"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        public static void RemoveWhere<T>(Expression<Func<T, bool>> expression = null)
        {
            Type myObjectType = typeof(T);
            StringBuilder command = new StringBuilder();
            var connectionName = Model.ConnectionName(myObjectType);
            var sqlDialectider = SqlDialectVariantFactory.GetSqlDialect(myObjectType);
            using (var cmd = Execute.Command(connectionName))
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
                    visitor.Dialect = sqlDialectider;
                    visitor.Visit(body);
                    Execute.Utility.AddParametersToCommand(parameters, cmd, connectionName);
                }

                cmd.CommandText = command.ToString();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Get an instance of the <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static T Get<T>(dynamic parameters = null) where T : new()
        {
            if (ReflectionCache.HasInterface(typeof(T), typeof(IGet<T>)))
                return ((IGet<T>)ReflectionCache.GetNewObject<T>()).Get(parameters);
            
            if (parameters == null)
                throw new ArgumentNullException("parameters");

            var obj = new Query<T>().Where(parameters).Limit(1).Execute();

            return System.Linq.Enumerable.FirstOrDefault(obj);
        }

        /// <summary>
        /// Get all the <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> All<T>() where T : new()
        {
            if (ReflectionCache.HasInterface(typeof(T), typeof(IGetMany<T>)))
            {
                return ((IGetMany<T>)ReflectionCache.GetNewObject<T>()).GetMany() as IEnumerable<T>;
            }

            return new Query<T>().Execute();
        }

        #region Utility
        private static void AddColumnParameter(ColumnMember cm, object myObject, DbCommand cmd, String connectionName)
        {
            object value = cm.Property.GetValue(myObject, null).DBNullCoalese();

            if (!Model.CheckNullStateForColumn(cm, value))
                throw new InvalidColumnDataException(cm.ColumnAttribute, value);

            if (!Model.CheckLengthForColumn(cm, value))
                if (cm.ColumnAttribute.MaxStringLength != default(Int32) && cm.ColumnAttribute.ShouldTrimLongStrings && value != null)
                    value = value.ToString().Substring(0, cm.ColumnAttribute.MaxStringLength - 1);
                else
                    throw new InvalidColumnDataException(cm.ColumnAttribute, value);

            if (cm.ColumnAttribute.SqlDbType == null)
                Execute.Utility.AddParameterToCommand(cm.Name, value, cmd, connectionName);
            else
                Execute.Utility.AddParameterToCommand(cm.Name, value, cmd, connectionName, cm.ColumnAttribute.SqlDbType);
        }
        #endregion
    }
}