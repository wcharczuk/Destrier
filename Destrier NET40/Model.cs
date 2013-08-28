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
            var ta = ReflectionCache.GetTableAttribute(t);

            if (ta == null)
                return null;

            if (!String.IsNullOrEmpty(ta.TableName))
                return ta.TableName;
            else
                return t.Name;
        }

        public static String TableNameFullyQualified(Type t)
        {
            return String.Format("{0}.{1}.{2}", DatabaseName(t), SchemaName(t), TableName(t));
        }

        public static String DatabaseName(Type t)
        {
            var databaseName = ReflectionCache.GetTableAttribute(t).DatabaseName;
            return !String.IsNullOrEmpty(databaseName) ? databaseName : DatabaseConfigurationContext.DefaultDatabaseName;
        }

        public static String SchemaName(Type t)
        {
            var schemaName = ReflectionCache.GetTableAttribute(t).SchemaName;
            return schemaName ?? DatabaseConfigurationContext.DefaultSchemaName ?? "dbo"; //the mssql backup.
        }

        public static Boolean UseNoLock(Type t)
        {
            return ReflectionCache.GetTableAttribute(t).UseNoLock;
        }

        public static String ColumnName(PropertyInfo pi)
        {
            ColumnAttribute ca = ReflectionCache.GetColumnAttribute(pi);

            if (ca == null) //it's not a column!
                return null;

            if (!String.IsNullOrEmpty(ca.Name))
                return ca.Name;
            else
                return pi.Name;
        }

        public static ColumnMember ColumnMemberForPropertyName(Type type, String propertyName)
        {
            return ReflectionCache.GetColumnMemberStandardizedLookup(type)[Model.StandardizeCasing(propertyName)];
        }

        public static String ConnectionString(Type t)
        {
            TableAttribute ta = ReflectionCache.GetTableAttribute(t);

            if (ta == null)
                throw new InvalidOperationException("Base Model classes must have a 'Table' attribute specifying the relation in the database to interact with!");

            if (!String.IsNullOrEmpty(ta.ConnectionName))
                return DatabaseConfigurationContext.ConnectionStrings[ta.ConnectionName];
            else if (!String.IsNullOrEmpty(DatabaseConfigurationContext.DefaultConnectionString))
                return DatabaseConfigurationContext.DefaultConnectionString;
            else
                throw new InvalidOperationException("No connection string for object.");
        }

        public static String ConnectionName(Type t)
        {
            TableAttribute ta = ReflectionCache.GetTableAttribute(t);
            return ta.ConnectionName ?? DatabaseConfigurationContext.DefaultConnectionName;
        }

        public static ColumnMember[] ColumnsNonPrimaryKey(Type t)
        {
            return ReflectionCache.GetColumnMembers(t).Where(cm => !cm.IsPrimaryKey).ToArray();
        }

        public static ColumnMember[] ColumnsPrimaryKey(Type t)
        {
            return ReflectionCache.GetColumnMembers(t).Where(cm => cm.IsPrimaryKey).ToArray();
        }

        public static ColumnMember AutoIncrementColumn(Type t)
        {
            foreach (ColumnMember pi in ColumnsPrimaryKey(t))
            {
                if (pi.IsAutoIdentity)
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
            if (ReflectionCache.GetTableAttribute(t) != null)
            {
                var databaseColumns = Schema.GetColumnsForTable(Model.TableName(t), Model.DatabaseName(t), Model.ConnectionString(t));
                var columnMembers = ReflectionCache.GetColumnMembers(t);

                foreach (var cm in columnMembers)
                {
                    var modelColumn = cm.ColumnAttribute;
                    if (!modelColumn.IsForReadOnly)
                    {
                        if (!databaseColumns.Any(c => c.Name.Equals(cm.Name, StringComparison.InvariantCultureIgnoreCase)))
                            throw new ColumnMissingException(String.Format("\'{0}\' : Column in the model doesn't map to the schema.", cm.Name));

                        var databaseColumn = databaseColumns.FirstOrDefault(c => c.Name.Equals(cm.Name, StringComparison.InvariantCultureIgnoreCase));

                        if (!modelColumn.IsPrimaryKey && databaseColumn.CanBeNull != modelColumn.CanBeNull)
                            throw new ColumnNullabilityException(String.Format("{4} DBColumn: {0} {2} ModelColumn: {1} {3}", databaseColumn.Name, cm.Name, databaseColumn.CanBeNull.ToString(), modelColumn.CanBeNull.ToString(), Model.TableName(t)));
                    }
                }

                foreach (var column in databaseColumns)
                {
                    if (!column.IsForReadOnly)
                    {
                        if (!columnMembers.Any(c => c.Name.Equals(column.Name, StringComparison.InvariantCultureIgnoreCase)))
                            throw new ColumnMissingException(String.Format("\'{0}\' : Column in the schema ({1}) doesn't map to the model.", column.Name, Model.TableName(t)));
                    }
                }
            }
        }

        public static Boolean CheckNullStateForColumn(ColumnMember cm, object value)
        {
            if (!cm.ColumnAttribute.CanBeNull)
                return ReflectionCache.IsSet(value);
            else
                return true;
        }

        public static Boolean CheckLengthForColumn(ColumnMember cm, object value)
        {
            if (value == null)
                return true;

            if (value is String && cm.ColumnAttribute.MaxStringLength != default(Int32))
                return value.ToString().Length <= cm.ColumnAttribute.MaxStringLength;
            else
                return true;
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
            return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToLower(input);
        }

		public static void Populate(object instance, IndexedSqlDataReader dr)
		{
			var thisType = instance.GetType();
            var members = new Dictionary<String, ColumnMember>();
            if (dr.StandardizeCasing)
                members = ReflectionCache.GetColumnMemberStandardizedLookup(thisType);
            else
                members = ReflectionCache.GetColumnMemberLookup(thisType);

			foreach (ColumnMember col in members.Values)
			{
				col.SetValue(instance, dr.Get(col));
			}
		}

        public static void PopulateFullResults(object instance, IndexedSqlDataReader dr, Type thisType, Member rootMember = null, ReferencedObjectMember parentMember = null, Dictionary<Type, Dictionary<Object, Object>> objectLookups = null)
        {
            if (dr.HasReferencedObjectMembers || parentMember != null)
            {
                var members = ReflectionCache.GetColumnMembers(thisType);
                foreach (ColumnMember col in members)
                {
                    var columnName = col.Name;
                    if (parentMember != null)
                        columnName = String.Format("{0}.{1}", parentMember.FullyQualifiedName, col.Name);

                    col.SetValue(instance, dr.Get(col, columnName));
                }

                rootMember = rootMember ?? new RootMember(thisType);
                foreach (ReferencedObjectMember rom in ReflectionCache.Members(thisType, rootMember, parentMember).Where(m => m is ReferencedObjectMember && !m.ParentAny(p => p is ChildCollectionMember)))
                {
                    var type = rom.Type;
                    
                    if (rom.IsLazy)
                    {
                        var mi = typeof(ReflectionCache).GetMethod("GenerateLazyReferencedObjectMember");
                        var genericMi = mi.MakeGenericMethod(rom.UnderlyingGenericType);
                        var lazy = genericMi.Invoke(null, new Object[] { rom, instance });
                        rom.SetValue(instance, lazy);
                    }
                    else
                    {
                        var newObject = ReflectionCache.GetNewObject(type);

                        PopulateFullResults(newObject, dr, type, rootMember, rom);
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

                if (dr.HasChildCollectionMembers)
                {
                    foreach (ChildCollectionMember cm in ReflectionCache.Members(thisType, rootMember, parentMember).Where(m => m is ChildCollectionMember && m.IsLazy))
                    {
                        var mi = typeof(ReflectionCache).GetMethod("GenerateLazyChildCollectionMember");
                        var genericMi = mi.MakeGenericMethod(cm.UnderlyingGenericType);
                        var lazy = genericMi.Invoke(null, new Object[] { cm, instance });
                        cm.SetValue(instance, lazy);
                    }
                }
            }
            else //this is the fast pipeline.
            {
                dr.SetInstanceValues(instance);
            }

            if (dr.HasChildCollectionMembers && objectLookups != null)
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

        public static String GenerateAlias()
        {
            return System.Guid.NewGuid().ToString("N");
        }

        public static Member MemberForPropertyName(String propertyName, Dictionary<String, Member> members)
        {
            Member member;
            members.TryGetValue(propertyName, out member);

            //check if casing was an issue ....
            if (member == null)
            {
                member = members.Values.FirstOrDefault(m => m.FullyQualifiedName.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase));
            }

            return member;
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

            Member member;
            members.TryGetValue(fullName, out member);
            return member;
        }
    }
}