using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;

namespace Destrier
{
    /// <summary>
    /// The model represents functions pertaining to reflection on objects used in the ORM.
    /// </summary>
    public static class Model
    {
        public static String TableName(Type t)
        {
            return TableAttribute(t).TableName;
        }

        public static String TableNameFullyQualified(Type t)
        {
            return String.Format("{0}.{1}.{2}", DatabaseName(t), SchemaName(t), TableName(t));
        }

        public static String DatabaseName(Type t)
        {
            var databaseName = TableAttribute(t).DatabaseName;
            return !String.IsNullOrEmpty(databaseName) ? databaseName : DatabaseConfigurationContext.DefaultDatabaseName;
        }

        public static String SchemaName(Type t)
        {
            var schemaName = TableAttribute(t).SchemaName;
            return !String.IsNullOrEmpty(schemaName) ? schemaName : DatabaseConfigurationContext.DefaultSchemaName;
        }

        public static String ColumnName(PropertyInfo pi)
        {
            ColumnAttribute ca = ColumnAttribute(pi);

            if (ca == null) //it's not a column!
                return null;

            if (!String.IsNullOrEmpty(ca.Name))
                return ca.Name;
            else
                return pi.Name;
        }

        public static String ColumnName(ColumnAttribute ca, String propertyName)
        {
            if (ca == null)
                return null;

            if (!String.IsNullOrEmpty(ca.Name))
                return ca.Name;

            return propertyName;
        }

        public static String ColumnNameForPropertyName(Type type, String propertyName)
        {
            var propertyInfo = ColumnPropertyForPropertyName(type, propertyName);

            if (propertyInfo == null)
                return null;

            return ColumnName(propertyInfo);
        }

        public static ColumnAttribute ColumnAttributeForPropertyName(Type type, String propertyName)
        {
            var propertyInfo = ColumnPropertyForPropertyName(type, propertyName);

            if (propertyInfo == null)
                return null;

            return ColumnAttribute(propertyInfo);
        }

        public static PropertyInfo ColumnPropertyForPropertyName(Type type, String propertyName)
        {
            var propertyInfo = ReflectionCache.GetColumns(type).Where(p => p.Name.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            return propertyInfo;
        }

        public static ColumnAttribute ColumnAttribute(PropertyInfo pi)
        {
            return ReflectionCache.GetColumnAttribute(pi);
        }

        public static TableAttribute TableAttribute(Type t)
        {
            return ReflectionCache.GetTableAttribute(t);
        }

        public static String ConnectionString(Type t)
        {
            TableAttribute ta = TableAttribute(t);

            if (ta == null)
                throw new InvalidOperationException("Base Model classes must have a 'Table' attribute specifying the relation in the database to interact with!");

            if (!String.IsNullOrEmpty(ta.ConnectionStringName))
                return DatabaseConfigurationContext.ConnectionStrings[ta.ConnectionStringName];
            else if (!String.IsNullOrEmpty(DatabaseConfigurationContext.DefaultConnectionString))
                return DatabaseConfigurationContext.DefaultConnectionString;
            else
                throw new InvalidOperationException("No connection string for object.");
        }

        public static PropertyInfo[] Columns(Type t)
        {
            return ReflectionCache.GetColumns(t);
        }

        public static Dictionary<String, ColumnMember> ColumnMembersStandardized(Type t)
        {
            return ReflectionCache.GetColumnMembersStandardized(t);
        }

        public static Dictionary<String, ColumnMember> ColumnMembers(Type t)
        {
            return ReflectionCache.GetColumnMembers(t);
        }

        public static PropertyInfo[] ReferencedObjectProperties(Type t)
        {
            return ReflectionCache.GetReferencedObjectProperties(t);
        }

        public static PropertyInfo[] ChildCollectionProperties(Type t)
        {
            return ReflectionCache.GetChildCollectionProperties(t);
        }

        public static ColumnAttribute[] ColumnAttributes(Type t)
        {
            return ReflectionCache.GetColumnAttributes(t);
        }

        public static PropertyInfo[] ColumnsNonPrimaryKey(Type t)
        {
            return ReflectionCache.GetColumnsNonPrimaryKey(t);
        }

        public static PropertyInfo[] ColumnsPrimaryKey(Type t)
        {
            return ReflectionCache.GetColumnsPrimaryKey(t);
        }

        public static List<String> DatabaseColumnNames(Type t)
        {
            List<String> dbColumnNames = new List<String>();
            foreach (PropertyInfo pi in Columns(t))
            {
                dbColumnNames.Add("[" + ColumnName(pi) + "]");
            }
            return dbColumnNames;
        }

        public static List<String> DatabaseColumnNamesPrimaryKey(Type t)
        {
            List<String> dbColumnNames = new List<String>();
            foreach (PropertyInfo pi in ColumnsPrimaryKey(t))
            {
                dbColumnNames.Add("[" + ColumnName(pi) + "]");
            }
            return dbColumnNames;
        }

        public static List<String> DatabaseColumNamesNonPrimaryKey(Type t)
        {
            List<String> dbColumnNames = new List<String>();
            foreach (PropertyInfo pi in ColumnsNonPrimaryKey(t))
            {
                dbColumnNames.Add("[" + ColumnName(pi) + "]");
            }
            return dbColumnNames;
        }

        public static List<String> DatabaseParameterNames(Type t)
        {
            List<String> dbColumnNames = new List<String>();
            foreach (PropertyInfo pi in Columns(t))
            {
                dbColumnNames.Add(ColumnName(pi));
            }
            return dbColumnNames;
        }

        public static List<String> DatabaseParameterNamesPrimaryKey(Type t)
        {
            List<String> dbColumnNames = new List<String>();
            foreach (PropertyInfo pi in ColumnsPrimaryKey(t))
            {
                dbColumnNames.Add(ColumnName(pi));
            }
            return dbColumnNames;
        }

        public static List<String> DatabaseParameterNamesNonPrimaryKey(Type t)
        {
            List<String> dbColumnNames = new List<String>();
            foreach (PropertyInfo pi in ColumnsNonPrimaryKey(t))
            {
                dbColumnNames.Add(ColumnName(pi));
            }
            return dbColumnNames;
        }

        public static String ReferencedObjectColumnName(PropertyInfo pi)
        {
            var roca = ReferencedObjectAttribute(pi);

            if (roca == null)
                return null;

            return roca.PropertyName.ToLowerCaseFirstLetter();
        }

        public static ReferencedObjectAttribute ReferencedObjectAttribute(PropertyInfo pi)
        {
            return ReflectionCache.GetReferencedObjectAttribute(pi);
        }

        public static ChildCollectionAttribute ChildCollectionAttribute(PropertyInfo pi)
        {
            return ReflectionCache.GetChildCollectionAttribute(pi);
        }

        public static Boolean HasReferencedObjects(Type t)
        {
            return ReflectionCache.HasReferencedObjectProperties(t);
        }

        public static Boolean HasChildCollections(Type t)
        {
            return ReflectionCache.HasChildCollectionProperties(t);
        }

        public static PropertyInfo AutoIncrementColumn(Type t)
        {
            foreach (PropertyInfo pi in ColumnsPrimaryKey(t))
            {
                var ca = ColumnAttribute(pi);
                if (ca.IsAutoIdentity)
                    return pi;
            }
            return null;
        }

        public static Boolean HasAutoIncrementColumn(Type t)
        {
            return AutoIncrementColumn(t) != null;
        }

        public static void CheckColumns<T>()
        {
            CheckColumns(typeof(T));
        }

        public static void CheckColumns(Type t)
        {
            var databaseColumns = Schema.GetColumnsForTable(Model.TableName(t), Model.DatabaseName(t), Model.ConnectionString(t));
            var columnProperties = Model.Columns(t);

            foreach (var columnProperty in columnProperties)
            {
                var modelColumn = Model.ColumnAttribute(columnProperty);
                if (!modelColumn.IsForReadOnly)
                {
                    if (!databaseColumns.Any(c => c.Name.Equals(Model.ColumnName(columnProperty), StringComparison.InvariantCultureIgnoreCase)))
                        throw new ColumnMissingException(String.Format("\'{0}\' : Column in the model doesn't map to the schema.", Model.ColumnName(columnProperty)));

                    var databaseColumn = databaseColumns.FirstOrDefault(c => c.Name.Equals(Model.ColumnName(columnProperty), StringComparison.InvariantCultureIgnoreCase));

                    if (!databaseColumn.CanBeNull && !modelColumn.IsPrimaryKey && modelColumn.CanBeNull)
                        throw new ColumnNullabilityException(String.Format("{4} DBColumn: {0} {2} ModelColumn: {1} {3}", databaseColumn.Name, Model.ColumnName(columnProperty), databaseColumn.CanBeNull.ToString(), modelColumn.CanBeNull.ToString(), Model.TableName(t)));
                }
            }

            foreach (var column in databaseColumns)
            {
                if (!column.IsForReadOnly)
                {
                    if (!columnProperties.Any(c => Model.ColumnName(c).Equals(column.Name, StringComparison.InvariantCultureIgnoreCase)))
                        throw new ColumnMissingException(String.Format("\'{0}\' : Column in the schema ({1}) doesn't map to the model.", column.Name, Model.TableName(t)));

                    var matchingColumn = columnProperties.FirstOrDefault(c => Model.ColumnName(c).Equals(column.Name, StringComparison.InvariantCultureIgnoreCase));
                    var ca = Model.ColumnAttribute(matchingColumn);
                }
            }
        }

        public static Boolean CheckNullStateForColumn(ColumnAttribute column, object value)
        {
            if (!column.CanBeNull)
                return ReflectionCache.IsSet(value);
            else
                return true;
        }

        public static Boolean CheckLengthForColumn(ColumnAttribute column, object value)
        {
            if (value == null)
                return true;

            if (value is String && column.MaxStringLength != default(Int32))
                return value.ToString().Length <= column.MaxStringLength;
            else
                return true;
        }

        public static Type RootTypeForExpression(Expression exp)
        {
            if (exp.NodeType == ExpressionType.MemberAccess)
            {
                var visitedMemberExp = exp as MemberExpression;
                if (visitedMemberExp.Expression != null)
                {
                    while (visitedMemberExp.Expression.NodeType == ExpressionType.MemberAccess)
                    {
                        if (visitedMemberExp.Expression.NodeType == ExpressionType.MemberAccess)
                        {
                            visitedMemberExp = visitedMemberExp.Expression as MemberExpression;
                        }
                    }

                    if (visitedMemberExp.Expression.NodeType == ExpressionType.Parameter)
                    {
                        return visitedMemberExp.Expression.Type;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else if (exp.NodeType == ExpressionType.Parameter || exp.NodeType == ExpressionType.Constant)
            {
                return exp.Type;
            }

            return null;
        }

        public static Member MemberForExpression(MemberExpression memberExp, Dictionary<String, Member> members)
        {
            List<String> visitedNames = new List<String>();

            var com = new ColumnMember(memberExp.Member as PropertyInfo);

            visitedNames.Add(com.Name);

            var visitedMemberExp = memberExp;
            while (visitedMemberExp.Expression.NodeType == ExpressionType.MemberAccess)
            {
                visitedMemberExp = memberExp.Expression as MemberExpression;
                if (visitedMemberExp.Member is PropertyInfo)
                {
                    ReferencedObjectMember rom = new ReferencedObjectMember(visitedMemberExp.Member as PropertyInfo);
                    visitedNames.Add(rom.Name);
                }
                else
                    return null; //abort!
            }

            visitedNames.Reverse();
            var fullName = String.Join(".", visitedNames);
            return members[fullName];
        }

        public static String InstancePrimaryKeyValue(Type t, Object instance)
        {
            var primaryKeys = Model.ColumnsPrimaryKey(t);
            String objPrimaryKeyValue = null;
            if (primaryKeys.Count() > 1)
            {
                var pkValues = new List<String>();
                foreach (var pi in primaryKeys)
                {
                    object value = pi.GetValue(instance);
                    objPrimaryKeyValue = value != null ? value.ToString() : null;
                    pkValues.Add(objPrimaryKeyValue.ToString());
                }
                objPrimaryKeyValue = String.Join("|", pkValues);
            }
            else if (primaryKeys.Any())
            {
                var objPrimaryKeyProperty = primaryKeys.First();
                var value = objPrimaryKeyProperty.GetValue(instance);
                objPrimaryKeyValue = value != null ? value.ToString() : null;
            }
            return objPrimaryKeyValue;
        }

        public static String StandardizeCasing(String input)
        {
            return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToUpper(input);
        }

        public static void PopulateFullResults(BaseModel instance, IndexedSqlDataReader dr, Type thisType, Member rootMember = null, ReferencedObjectMember parentMember = null, Dictionary<Type, Dictionary<Object, Object>> objectLookups = null)
        {
            var members = Model.ColumnMembers(thisType);

            if (Model.HasReferencedObjects(thisType) || parentMember != null)
            {
                foreach (ColumnMember col in members.Values)
                {
                    var columnName = col.Name;
                    if (parentMember != null)
                        columnName = String.Format("{0}.{1}", parentMember.FullyQualifiedName, col.Name);

                    col.SetValue(instance, dr.Get(col.Type, columnName));
                }

                rootMember = rootMember ?? new RootMember(thisType);
                foreach (ReferencedObjectMember rom in ReflectionCache.Members(thisType, rootMember, parentMember).Where(m => m is ReferencedObjectMember && !m.ParentAny(p => p is ChildCollectionMember)))
                {
                    var type = rom.Type;
                    var newObject = ReflectionCache.GetNewObject(type);
                    PopulateFullResults((newObject as BaseModel), dr, type, rootMember, rom);
                    rom.Property.SetValue(instance, newObject);

                    if (objectLookups != null)
                    {
                        if (!objectLookups.ContainsKey(rom.Type))
                        {
                            objectLookups.Add(rom.Type, new Dictionary<Object, Object>());
                        }
                        var pkv = Model.InstancePrimaryKeyValue(rom.Type, newObject);
                        if (pkv != null && !objectLookups[rom.Type].ContainsKey(pkv))
                        {
                            objectLookups[rom.Type].Add(pkv, newObject);
                        }
                    }
                }
            }
            else
            {
                for (int x = 0; x < dr.FieldCount; x++)
                {
                    var name = dr.ColumnIndexMap[x];
                    ColumnMember member = null;
                    members.TryGetValue(name, out member);
                    if (member != null)
                    {
                        var value = dr.Get(member.Type, x);
                        if (!(value is DBNull))
                            member.SetValue(instance, value);
                    }
                }
            }

            if (Model.HasChildCollections(thisType) && objectLookups != null)
            {
                if (!objectLookups.ContainsKey(thisType))
                {
                    objectLookups.Add(thisType, new Dictionary<Object, Object>());
                }
                var pkv = Model.InstancePrimaryKeyValue(thisType, instance);
                if (pkv != null && !objectLookups[thisType].ContainsKey(pkv))
                {
                    objectLookups[thisType].Add(pkv, instance);
                }
            }
        }
    }
}