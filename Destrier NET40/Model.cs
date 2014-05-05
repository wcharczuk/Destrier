using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using System.Reflection.Emit;

namespace Destrier
{
    public class ModelReflection
    {
        public static TableAttribute TableAttribute(Type t)
        {
            return t.GetCustomAttributes(true).FirstOrDefault(at => at is TableAttribute) as TableAttribute;
        }

        public static ColumnAttribute ColumnAttribute(PropertyInfo property)
        {
            return property.GetCustomAttributes(typeof(ColumnAttribute), false).FirstOrDefault() as ColumnAttribute;
        }

        public static ReferencedObjectAttribute ReferencedObjectAttribute(PropertyInfo property)
        {
            return property.GetCustomAttributes(typeof(ReferencedObjectAttribute), false).FirstOrDefault() as ReferencedObjectAttribute;
        }

        public static ChildCollectionAttribute ChildCollectionAttribute(PropertyInfo property)
        {
            return property.GetCustomAttributes(typeof(ChildCollectionAttribute), false).FirstOrDefault() as ChildCollectionAttribute;
        }
    }

    public delegate void SetInstanceValuesDelegate(IndexedSqlDataReader dr, object instance);

    /// <summary>
    /// The model represents functions pertaining to reflection on objects used in the ORM.
    /// </summary>
    public static class Model
    {
        #region Text Stuff

        public static String StandardizeCasing(String input)
        {
            return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToLower(input);
        }

        public static String GenerateAlias()
        {
            return System.Guid.NewGuid().ToString("N");
        }

        #endregion

        public static String TableName(Type t)
        {
            var ta = ModelCache.GetTableAttribute(t);

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
            var databaseName = ModelCache.GetTableAttribute(t).DatabaseName;
            return !String.IsNullOrEmpty(databaseName) ? databaseName : DatabaseConfigurationContext.DefaultDatabaseName;
        }

        public static String SchemaName(Type t)
        {
            var schemaName = ModelCache.GetTableAttribute(t).SchemaName;
            return schemaName ?? DatabaseConfigurationContext.DefaultSchemaName ?? "dbo";
        }

        public static Boolean UseNoLock(Type t)
        {
            return ModelCache.GetTableAttribute(t).UseNoLock;
        }

        public static String ColumnName(PropertyInfo pi)
        {
            var ca = ModelReflection.ColumnAttribute(pi);

            if (ca == null) //it's not a column!
                return null;

            if (!String.IsNullOrEmpty(ca.Name))
                return ca.Name;
            else
                return pi.Name;
        }

        public static Boolean IsModel(Type t)
        {
            var table_attribute = ModelCache.GetTableAttribute(t);
            var column_attributes = ModelCache.GetColumnMembers(t);

            return table_attribute != null && column_attributes != null && column_attributes.Any();
        }

        public static ColumnMember ColumnMemberForPropertyName(Type type, String propertyName)
        {
            return ModelCache.GetColumnMemberStandardizedLookup(type)[Model.StandardizeCasing(propertyName)];
        }

        public static String ConnectionString(Type t)
        {
            var ta = ModelCache.GetTableAttribute(t);

            if (ta == null)
                throw new SchemaMetadataException("Base Model classes must have a 'Table' attribute specifying the relation in the database to interact with!");

            if (!String.IsNullOrEmpty(ta.ConnectionName))
                return DatabaseConfigurationContext.ConnectionStrings[ta.ConnectionName];
            else if (!String.IsNullOrEmpty(DatabaseConfigurationContext.DefaultConnectionString))
                return DatabaseConfigurationContext.DefaultConnectionString;
            else
                throw new SchemaMetadataException("No connection string for object.");
        }

        public static String ConnectionName(Type t)
        {
            TableAttribute ta = ModelCache.GetTableAttribute(t);
            return ta.ConnectionName ?? DatabaseConfigurationContext.DefaultConnectionName;
        }

        public static ColumnMember[] ColumnsNonPrimaryKey(Type t)
        {
            return ModelCache.GetColumnMembers(t).Where(cm => !cm.IsPrimaryKey).ToArray();
        }

        public static ColumnMember[] ColumnsPrimaryKey(Type t)
        {
            return ModelCache.GetColumnMembers(t).Where(cm => cm.IsPrimaryKey).ToArray();
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

		public static void Populate(object instance, IndexedSqlDataReader dr)
		{
			var thisType = instance.GetType();
            var members = ModelCache.GetColumnMemberLookup(thisType);

			foreach (var col in members.Values)
			{
				col.SetValue(instance, dr.Get(col));
			}
		}

        public static Member MemberForPropertyName(String propertyName, Dictionary<String, Member> members)
        {
            Member member;
            members.TryGetValue(propertyName, out member);

            if (member == null)
            {
                member = members.Values.FirstOrDefault(m => m.FullyQualifiedName.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase));
            }

            return member;
        }

        public static Member MemberForExpression(MemberExpression memberExp, Dictionary<String, Member> members)
        {
            var visitedNames = new List<String>();

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
                {
                    return null; //abort!
                }
            }

            visitedNames.Reverse();
            var fullName = String.Join(".", visitedNames);

            Member member;
            members.TryGetValue(fullName, out member);
            return member;
        }

        public static Dictionary<String, ColumnMember> GenerateColumnMemberStandardizedLookup(Type type)
        {
            return GenerateColumnMembers(type).ToDictionary(cm => Model.StandardizeCasing(cm.Name));
        }

        public static ColumnMember[] GenerateColumnMembers(Type type)
        {
            PropertyInfo[] properties = ModelCache.GetColumnMemberProperties(type);
            var members = new List<ColumnMember>();

            foreach (var prop in properties)
                members.Add(new ColumnMember(prop));

            return members.ToArray();
        }

        public static ReferencedObjectMember[] GenerateReferencedObjectMembers(Type type)
        {
            var list = new List<ReferencedObjectMember>();
            PropertyInfo[] properties = ModelCache.GetReferencedObjectMemberProperties(type);
            foreach (var prop in properties)
                list.Add(new ReferencedObjectMember(prop));

            return list.ToArray();
        }

        public static ChildCollectionMember[] GenerateChildCollectionMembers(Type type)
        {
            var list = new List<ChildCollectionMember>();
            PropertyInfo[] properties = ModelCache.GetChildCollectionMemberProperties(type);
            foreach (var prop in properties)
                list.Add(new ChildCollectionMember(prop));

            return list.ToArray();
        }

        public static RootMember GenerateRootMemberForType(Type type)
        {
            return new RootMember(type) { TableAlias = Model.GenerateAlias(), OutputTableName = Model.GenerateAlias(), UseNoLock = Model.UseNoLock(type) };
        }

        public static List<Member> GenerateMembers(Type type, Member rootMember = null, Member parentMember = null)
        {
            List<Member> members = new List<Member>();
            foreach (var cm in GenerateColumnMembers(type))
            {
                if (!cm.Skip)
                {
                    cm.Parent = parentMember;
                    cm.Root = rootMember;
                    members.Add(cm);
                }
            }
            if (ModelCache.HasReferencedObjectMembers(type))
            {
                foreach (var rom in GenerateReferencedObjectMembers(type))
                {
                    rom.Parent = parentMember;
                    rom.Root = rootMember;
                    members.Add(rom);
                }
            }
            if (ModelCache.HasChildCollectionMembers(type))
            {
                foreach (var ccm in GenerateChildCollectionMembers(type))
                {
                    ccm.Parent = parentMember;
                    ccm.Root = rootMember;
                    members.Add(ccm);
                }
            }
            return members;
        }

        public static List<Member> GenerateAllMembers(Type type)
        {
            var members = GenerateAllMembers(type, GenerateRootMemberForType(type), null);
            return members.ToList();
        }

        public static List<Member> GenerateAllMembers(Type type, Member rootMember, Member parentMember)
        {
            var members = new List<Member>();
            rootMember = rootMember ?? GenerateRootMemberForType(type);
            _generateMembersImpl(type, members, rootMember, parentMember);
            return members.ToList();
        }

        public static Lazy<T> GenerateLazyReferencedObjectMember<T>(ReferencedObjectMember rom, object instance) where T : new()
        {
            Func<T> func = () =>
            {
                var value = rom.ReferencedColumnProperty.GetValue(instance);
                return Database.Get<T>(value);
            };

            return new Lazy<T>(func);
        }

        public static Lazy<T> GenerateLazyChildCollectionMember<T>(ChildCollectionMember cm, object instance) where T : class, new()
        {
            var parentReferencedValue = cm.ParentReferencedProperty.GetValue(instance);
            var referencedColumnName = cm.ReferencedColumnName;

            Func<T> func = () =>
            {
                dynamic queryParams = new System.Dynamic.ExpandoObject();
                ((IDictionary<String, object>)queryParams).Add(referencedColumnName, parentReferencedValue);

                var mi = typeof(Model).GetMethod("_runLazyChildCollectionQuery", BindingFlags.NonPublic | BindingFlags.Static);
                var genericMi = mi.MakeGenericMethod(cm.CollectionType);
                return genericMi.Invoke(null, new Object[] { queryParams }) as T;
            };

            return new Lazy<T>(func);
        }

        public static SetInstanceValuesDelegate GenerateSetInstanceValuesDelegate(IndexedSqlDataReader dr)
        {
            Type idr = typeof(IndexedSqlDataReader);
            var idr_methods = new Dictionary<String, MethodInfo>();
            foreach (var mi in idr.GetMethods())
            {
                if (mi.Name.StartsWith("Get") || mi.Name == "IsDBNull")
                {
                    if (!idr_methods.ContainsKey(mi.Name))
                        idr_methods.Add(mi.Name, mi);
                }
            }

            var get_value_fn = idr_methods["GetValue"];
            var get_guid_fn = idr_methods["GetGuid"];
            var is_dbnull = idr_methods["IsDBNull"];

            //use this with the type code on the dr field.
            var type_accessors = new Dictionary<TypeCode, MethodInfo>()
            {
                { TypeCode.Boolean, idr_methods["GetBoolean"] },
                { TypeCode.DateTime, idr_methods["GetDateTime"] },
                { TypeCode.UInt16, idr_methods["GetInt16"] },
                { TypeCode.Int16, idr_methods["GetInt16"] },
                { TypeCode.UInt32, idr_methods["GetInt32"] },
                { TypeCode.Int32, idr_methods["GetInt32"] },
                { TypeCode.UInt64, idr_methods["GetInt64"] },
                { TypeCode.Int64, idr_methods["GetInt64"] },
                { TypeCode.Single, idr_methods["GetDouble"] },
                { TypeCode.Double, idr_methods["GetDouble"] },
                { TypeCode.Decimal, idr_methods["GetDecimal"] },
                { TypeCode.String, idr_methods["GetString"] },
                { TypeCode.Char, idr_methods["GetString"] }, 
                { TypeCode.Byte, idr_methods["GetByte"] }
            };

            DynamicMethod dyn = new DynamicMethod("SetInstanceValues", typeof(void), new Type[] { typeof(IndexedSqlDataReader), typeof(object) });
            ILGenerator il = dyn.GetILGenerator();

            var defaults = new Dictionary<TypeCode, Action>() 
            {
                { TypeCode.Boolean, () => { il.Emit(OpCodes.Ldc_I4_0); } },
                { TypeCode.Int16, () => { il.Emit(OpCodes.Ldc_I4_0); } },
                { TypeCode.UInt16, () => { il.Emit(OpCodes.Ldc_I4_0); } },
                { TypeCode.Int32, () => { il.Emit(OpCodes.Ldc_I4_0); } },
                { TypeCode.UInt32, () => { il.Emit(OpCodes.Ldc_I4_0); } },
                { TypeCode.Int64, () => { il.Emit(OpCodes.Ldc_I8, 0); } },
                { TypeCode.UInt64, () => { il.Emit(OpCodes.Ldc_I8, 0); } },
                { TypeCode.Single, () => { il.Emit(OpCodes.Ldc_R4, 0.0f); } },
                { TypeCode.Double, () => { il.Emit(OpCodes.Ldc_R8, 0.0d); } },
                { TypeCode.String, () => { il.Emit(OpCodes.Ldnull); } },
            };

            var on_stack_conversions = new Dictionary<TypeCode, Action>()
            {
                { TypeCode.Boolean, () => { il.Emit(OpCodes.Conv_Ovf_I4); } },
                { TypeCode.Int32, () => { il.Emit(OpCodes.Conv_Ovf_I4); } } ,
                { TypeCode.SByte, () => { il.Emit(OpCodes.Conv_Ovf_I1); } } ,
                { TypeCode.Byte, () => { il.Emit(OpCodes.Conv_Ovf_I1_Un); } },
                { TypeCode.Int16, () => { il.Emit(OpCodes.Conv_Ovf_I2);} },
                { TypeCode.UInt16, () => { il.Emit(OpCodes.Conv_Ovf_I2_Un);} },
                { TypeCode.UInt32, () => { il.Emit(OpCodes.Conv_Ovf_I4_Un);} },
                { TypeCode.Int64, () => { il.Emit(OpCodes.Conv_Ovf_I8); } },
                { TypeCode.UInt64, () => {il.Emit(OpCodes.Conv_Ovf_I8_Un);} },
                { TypeCode.Single, () => { il.Emit(OpCodes.Conv_R4); } },
                { TypeCode.Double, () => { il.Emit(OpCodes.Conv_R8); } },
            };

            il.BeginExceptionBlock();
            var col_index = il.DeclareLocal(typeof(int));

            int index = 0;
            foreach (var c in dr.ColumnMemberIndexMap)
            {
                il.Emit(OpCodes.Ldc_I4, index);
                il.Emit(OpCodes.Stloc, col_index.LocalIndex);

                var setMethod = c.Property.GetSetMethod();
                if (c.IsNullableType)
                {
                    var nullable_local = il.DeclareLocal(c.Property.PropertyType);
                    var underlyingType = c.UnderlyingGenericType;

                    var origin = dr.GetFieldType(index);
                    var originType = Type.GetTypeCode(origin);

                    var nullable_constructor = c.Type.GetConstructors().First();

                    var was_not_null = il.DefineLabel();
                    var set_column = il.DefineLabel();

                    //load up our object .. again.
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Castclass, c.Property.DeclaringType);
                    //test if it was null

                    il.Emit(OpCodes.Ldarg_0);
                    _emitInt32(il, index);
                    il.Emit(OpCodes.Callvirt, is_dbnull);
                    il.Emit(OpCodes.Brfalse_S, was_not_null);

                    //new up a nullable<t>
                    il.Emit(OpCodes.Ldloca_S, nullable_local.LocalIndex);
                    il.Emit(OpCodes.Initobj, c.Type);
                    il.Emit(OpCodes.Ldloc, nullable_local.LocalIndex);
                    il.Emit(OpCodes.Br_S, set_column);

                    //grab the value
                    il.MarkLabel(was_not_null);
                    il.Emit(OpCodes.Ldarg_0);
                    _emitInt32(il, index);

                    MethodInfo get_value = null;
                    if (type_accessors.ContainsKey(originType))
                        get_value = type_accessors[originType];
                    else if (origin == typeof(Guid))
                        get_value = get_guid_fn;
                    else
                        get_value = get_value_fn;

                    il.EmitCall(OpCodes.Callvirt, get_value, null);
                    il.Emit(OpCodes.Newobj, nullable_constructor);

                    il.MarkLabel(set_column);
                    il.EmitCall(OpCodes.Callvirt, setMethod, null);
                }
                else
                {
                    var origin = dr.GetFieldType(index);
                    var originType = Type.GetTypeCode(origin);
                    var destinationType = Type.GetTypeCode(c.Type);

                    var was_not_null = il.DefineLabel();
                    var set_column = il.DefineLabel();

                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Castclass, c.Property.DeclaringType);

                    if (!c.ColumnAttribute.IsPrimaryKey && c.ColumnAttribute.CanBeNull)
                    {
                        il.Emit(OpCodes.Ldarg_0);
                        _emitInt32(il, index);
                        il.Emit(OpCodes.Callvirt, is_dbnull);
                        il.Emit(OpCodes.Brfalse_S, was_not_null);

                        //new up a default(T)
                        if (defaults.ContainsKey(destinationType))
                        {
                            defaults[destinationType]();
                        }
                        else
                        {
                            var local = il.DeclareLocal(c.Property.PropertyType);
                            il.Emit(OpCodes.Ldloca_S, local.LocalIndex);
                            il.Emit(OpCodes.Initobj, c.Property.PropertyType);
                            il.Emit(OpCodes.Ldloc, local.LocalIndex);
                        }
                        il.Emit(OpCodes.Br_S, set_column);
                        il.MarkLabel(was_not_null);
                    }

                    //get the value
                    il.Emit(OpCodes.Ldarg_0);
                    _emitInt32(il, index);

                    MethodInfo get_value = null;
                    if (type_accessors.ContainsKey(originType))
                    {
                        get_value = type_accessors[originType];
                    }
                    else if (origin == typeof(Guid))
                    {
                        get_value = get_guid_fn;
                    }
                    else
                    {
                        get_value = get_value_fn;
                    }

                    il.EmitCall(OpCodes.Callvirt, get_value, null);

                    if (originType != destinationType)
                    {
                        //we need to cast ...
                        if (destinationType == TypeCode.String)
                        {
                            var to_s = _getToStringForType(origin);
                            var local = il.DeclareLocal(origin);

                            il.Emit(OpCodes.Stloc, local.LocalIndex);
                            il.Emit(OpCodes.Ldloca_S, local.LocalIndex);
                            il.Emit(OpCodes.Call, to_s);
                        }
                        else
                        {
                            if (on_stack_conversions.ContainsKey(destinationType))
                            {
                                on_stack_conversions[destinationType]();
                            }
                            else //failsafe if we end up here, though doubtful ...
                            {
                                il.Emit(OpCodes.Newobj, c.Type);     
                            }
                        }
                    }
                    else if (originType == destinationType && c.Type.Equals(typeof(Byte[])))
                    {
                        il.Emit(OpCodes.Castclass, typeof(Byte[]));
                    }

                    il.MarkLabel(set_column);
                    il.EmitCall(OpCodes.Callvirt, setMethod, null);
                }
                index++;
            }

            var endLabel = il.DefineLabel();
            il.BeginCatchBlock(typeof(Exception));
            il.Emit(OpCodes.Ldloc, col_index.LocalIndex);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_0);
            il.EmitCall(OpCodes.Call, idr.GetMethod("ThrowDataException"), null);
            il.EndExceptionBlock();

            il.MarkLabel(endLabel);
            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ret);

            return (SetInstanceValuesDelegate)dyn.CreateDelegate(typeof(SetInstanceValuesDelegate));
        }

        static void _generateMembersImpl(Type type, List<Member> members, Member rootMember, Member parentMember = null)
        {
            foreach (var cm in GenerateColumnMembers(type))
            {
                if (!cm.Skip)
                {
                    cm.Parent = parentMember;
                    cm.Root = rootMember;
                    members.Add(cm);
                }
            }

            if (ModelCache.HasReferencedObjectMembers(type))
            {
                foreach (var rom in GenerateReferencedObjectMembers(type))
                {
                    rom.Parent = parentMember;
                    rom.Root = rootMember;

                    if (!rom.HasCycle)
                    {
                        members.Add(rom);
                        _generateMembersImpl(rom.Type, members, rootMember, rom);
                    }
                }
            }

            if (ModelCache.HasChildCollectionMembers(type))
            {
                foreach (var ccp in GenerateChildCollectionMembers(type))
                {
                    ccp.Parent = parentMember;
                    ccp.Root = rootMember;

                    if (parentMember != null && parentMember is ReferencedObjectMember)
                    {
                        ccp.ParentReferencedMember.Parent = parentMember;
                    }

                    if (!ccp.HasCycle)
                    {
                        members.Add(ccp);
                        _generateMembersImpl(ccp.CollectionType, members, rootMember, ccp);
                    }
                }
            }
        }

        static List<T> _runLazyChildCollectionQuery<T>(dynamic queryParams) where T : class, new()
        {
            return new Query<T>().Where(queryParams).Execute();
        }

        static MethodInfo _getToStringForType(Type type)
        {
            var methods = type.GetMethods();
            return methods.First(m => m.Name == "ToString");
        }

        static void _emitInt32(ILGenerator il, int value)
        {
            switch (value)
            {
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    break;
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    break;
                default:
                    if (value >= -128 && value <= 127)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4, value);
                    }
                    break;
            }
            return;
        }
    }

    public static class ModelValidation
    {

        public static void CheckColumns<T>()
        {
            CheckColumns(typeof(T));
        }

        public static void CheckColumns(Type t)
        {
            if (ModelCache.GetTableAttribute(t) != null)
            {
                var databaseColumns = Schema.GetColumnsForTable(Model.TableName(t), Model.DatabaseName(t), Model.ConnectionString(t));
                var columnMembers = Model.GenerateColumnMembers(t);

                foreach (var cm in columnMembers)
                {
                    var modelColumn = cm.ColumnAttribute;
                    if (!modelColumn.IsForReadOnly)
                    {
                        if (!databaseColumns.Any(c => c.Name.Equals(cm.Name, StringComparison.InvariantCultureIgnoreCase)))
                            throw new ColumnMissingException(String.Format("\'{0}\' : Column in the model doesn't map to the schema.", cm.Name));

                        var databaseColumn = databaseColumns.FirstOrDefault(c => c.Name.Equals(cm.Name, StringComparison.InvariantCultureIgnoreCase));

                        if (!modelColumn.IsPrimaryKey && databaseColumn.CanBeNull != modelColumn.CanBeNull)
                            throw new ColumnNullabilityException(String.Format("Column Nullability Mismatch : {4} DBColumn: {0} {2} ModelColumn: {1} {3}"
                                , databaseColumn.Name
                                , cm.Name
                                , databaseColumn.CanBeNull.ToString()
                                , modelColumn.CanBeNull.ToString()
                                , Model.TableName(t))
                            );
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
                return ReflectionHelper.IsSet(value);
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

    }
}