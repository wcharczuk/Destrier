using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.IO;
using System.Collections.Specialized;
using System.Data;
using System.Reflection.Emit;

namespace Destrier
{
    /// <summary>
    /// Set of utility functions used for reflection heavy operations.
    /// </summary>
    public class ModelCache
    {
        private static ConcurrentDictionary<Type, List<Member>> _memberCache = new ConcurrentDictionary<Type, List<Member>>();
        private static ConcurrentDictionary<Type, List<ColumnMember>> _columnMemberCache = new ConcurrentDictionary<Type, List<ColumnMember>>();

        private static ConcurrentDictionary<Type, Dictionary<String, ColumnMember>> _columnMemberLookupStandardizedCache = new ConcurrentDictionary<Type, Dictionary<String, ColumnMember>>();
        private static ConcurrentDictionary<Type, Dictionary<String, ColumnMember>> _columnMemberLookupCache = new ConcurrentDictionary<Type, Dictionary<String, ColumnMember>>();

        private static ConcurrentDictionary<Type, List<ReferencedObjectMember>> _referencedObjectMemberCache = new ConcurrentDictionary<Type, List<ReferencedObjectMember>>();
        private static ConcurrentDictionary<Type, List<ChildCollectionMember>> _childCollectionMemberCache = new ConcurrentDictionary<Type, List<ChildCollectionMember>>();

        private static ConcurrentDictionary<Type, List<Member>> _recursiveMemberCache = new ConcurrentDictionary<Type, List<Member>>();
        private static ConcurrentDictionary<Type, RootMember> _rootMemberCache = new ConcurrentDictionary<Type, RootMember>();

        private static ConcurrentDictionary<Type, TableAttribute> _tableAttributeCache = new ConcurrentDictionary<Type, TableAttribute>();
        private static ConcurrentDictionary<Type, bool> _hasChildCollectionPropertiesCache = new ConcurrentDictionary<Type, bool>();
        private static ConcurrentDictionary<Type, bool> _hasReferencedObjectPropertiesCache = new ConcurrentDictionary<Type, bool>();

        private static ConcurrentDictionary<Type, PropertyInfo[]> _columnMemberPropertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static ConcurrentDictionary<Type, PropertyInfo[]> _referencedObjectMemberPropertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static ConcurrentDictionary<Type, PropertyInfo[]> _childCollectionMemberPropertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static ConcurrentDictionary<String, SetInstanceValuesDelegate> _setInstanceValuesDelegateCache = new ConcurrentDictionary<String, SetInstanceValuesDelegate>();

        public delegate void SetInstanceValuesDelegate(IndexedSqlDataReader dr, object instance);

        public static Dictionary<String, ColumnMember> GetColumnMemberStandardizedLookup(Type type)
        {
            return _columnMemberLookupStandardizedCache.GetOrAdd(type, (t) =>
            {
                return GenerateColumnMembers(t).ToDictionary(cm => Model.StandardizeCasing(cm.Name));
            });
        }

        public static Dictionary<String, ColumnMember> GetColumnMemberLookup(Type type)
        {
            return _columnMemberLookupCache.GetOrAdd(type, (t) =>
            {
                return GenerateColumnMembers(t).ToDictionary(cm => cm.Name);
            });
        }

        public static List<ColumnMember> GetColumnMembers(Type type)
        {
            return _columnMemberCache.GetOrAdd(type, (t) =>
            {
                return GenerateColumnMembers(t);
            });
        }

        public static PropertyInfo[] GetColumnMemberProperties(Type type)
        {
            return _columnMemberPropertyCache.GetOrAdd(type, (t) =>
            {
                var list = new List<PropertyInfo>();
                PropertyInfo[] properties = ReflectionHelper.GetProperties(type).Values.ToArray();
                foreach (var prop in properties)
                {
                    if (prop.GetCustomAttributes(typeof(ColumnAttribute), false).Any())
                    {
                        list.Add(prop);
                    }
                }
                return list.ToArray();
            });
        }

        public static PropertyInfo[] GetReferencedObjectMemberProperties(Type type)
        {
            return _referencedObjectMemberPropertyCache.GetOrAdd(type, (t) =>
            {
                var list = new List<PropertyInfo>();
                PropertyInfo[] properties = ReflectionHelper.GetProperties(type).Values.ToArray();
                foreach (var prop in properties)
                {
                    if (prop.GetCustomAttributes(typeof(ReferencedObjectAttribute), false).Any())
                    {
                        list.Add(prop);
                    }
                }
                return list.ToArray();
            });
        }

        public static PropertyInfo[] GetChildCollectionMemberProperties(Type type)
        {
            return _childCollectionMemberPropertyCache.GetOrAdd(type, (t) =>
            {
                var list = new List<PropertyInfo>();
                PropertyInfo[] properties = ReflectionHelper.GetProperties(type).Values.ToArray();
                foreach (var prop in properties)
                {
                    if (prop.GetCustomAttributes(typeof(ChildCollectionAttribute), false).Any())
                    {
                        list.Add(prop);
                    }
                }
                return list.ToArray();
            });
        }

        public static List<ColumnMember> GenerateColumnMembers(Type type)
        {
            PropertyInfo[] properties = GetColumnMemberProperties(type);
            var members = new List<ColumnMember>();

            foreach (var prop in properties)
                members.Add(new ColumnMember(prop));

            return members;
        }

        public static List<ReferencedObjectMember> GetReferencedObjectMembers(Type type)
        {
            return _referencedObjectMemberCache.GetOrAdd(type, (t) =>
            {
                return GenerateReferencedObjectMembers(t);
            });
        }

        public static List<ReferencedObjectMember> GenerateReferencedObjectMembers(Type type)
        {
            var list = new List<ReferencedObjectMember>();
            PropertyInfo[] properties = GetReferencedObjectMemberProperties(type);
            foreach (var prop in properties)
                list.Add(new ReferencedObjectMember(prop));

            return list;
        }

        public static List<ChildCollectionMember> GetChildCollectionMembers(Type type)
        {
            return _childCollectionMemberCache.GetOrAdd(type, (t) =>
            {
                return GenerateChildCollectionMembers(t);
            });
        }

        public static List<ChildCollectionMember> GenerateChildCollectionMembers(Type type)
        {
            var list = new List<ChildCollectionMember>();
            PropertyInfo[] properties = GetChildCollectionMemberProperties(type);
            foreach (var prop in properties)
                list.Add(new ChildCollectionMember(prop));

            return list;
        }

        public static Boolean HasReferencedObjectMembers(Type type)
        {
            return _hasReferencedObjectPropertiesCache.GetOrAdd(type, (t) =>
            {
                return GetReferencedObjectMembers(t).Any();
            });
        }

        public static Boolean HasChildCollectionMembers(Type type)
        {
            return _hasChildCollectionPropertiesCache.GetOrAdd(type, (t) =>
            {
                return GetChildCollectionMembers(t).Any();
            });
        }

        #region Model Metadata

        public static ColumnAttribute GetColumnAttribute(PropertyInfo property)
        {
            return property.GetCustomAttributes(typeof(ColumnAttribute), false).FirstOrDefault() as ColumnAttribute;
        }

        public static ReferencedObjectAttribute GetReferencedObjectAttribute(PropertyInfo property)
        {
            return property.GetCustomAttributes(typeof(ReferencedObjectAttribute), false).FirstOrDefault() as ReferencedObjectAttribute;
        }

        public static ChildCollectionAttribute GetChildCollectionAttribute(PropertyInfo property)
        {
            return property.GetCustomAttributes(typeof(ChildCollectionAttribute), false).FirstOrDefault() as ChildCollectionAttribute;
        }

        public static TableAttribute GetTableAttribute(Type type)
        {
            return _tableAttributeCache.GetOrAdd(type, (t) =>
            {
                return t.GetCustomAttributes(true).FirstOrDefault(at => at is TableAttribute) as TableAttribute;
            });
        }

        #endregion

        #region General Reflection


        #endregion

        public static RootMember GetRootMemberForType(Type type)
        {
            return _rootMemberCache.GetOrAdd(type, (t) =>
            {
                return new RootMember(t) { TableAlias = Model.GenerateAlias(), OutputTableName = Model.GenerateAlias(), UseNoLock = Model.UseNoLock(t) };
            });
        }

        public static List<Member> Members(Type type, Member rootMember = null, Member parentMember = null)
        {
            List<Member> members = new List<Member>();
            foreach (ColumnMember cm in GetColumnMembers(type).Select(cm => cm.Clone()))
            {
                if (!cm.Skip)
                {
                    cm.Parent = parentMember;
                    cm.Root = rootMember;
                    members.Add(cm);
                }
            }
            if (HasReferencedObjectMembers(type))
            {
                foreach (ReferencedObjectMember rom in GetReferencedObjectMembers(type).Select(rom => rom.Clone()))
                {
                    rom.Parent = parentMember;
                    rom.Root = rootMember;
                    members.Add(rom);
                }
            }
            if (HasChildCollectionMembers(type))
            {
                foreach (ChildCollectionMember ccm in GetChildCollectionMembers(type).Select(ccm => ccm.Clone()))
                {
                    ccm.Parent = parentMember;
                    ccm.Root = rootMember;
                    members.Add(ccm);
                }
            }
            return members;
        }

        public static List<Member> MembersRecursiveCached(Type type)
        {
            return _recursiveMemberCache.GetOrAdd(type, (t) =>
            {
                return GenerateMembersRecursive(t);
            });
        }

        public static List<Member> GenerateMembersRecursive(Type type, Member rootMember = null, Member parent = null)
        {
            var members = new List<Member>();
            rootMember = rootMember ?? GetRootMemberForType(type);
            GenerateMembersImpl(type, members, rootMember, parent);
            return members.ToList();
        }

        private static void GenerateMembersImpl(Type type, List<Member> members, Member rootMember, Member parentMember = null)
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

            if (HasReferencedObjectMembers(type))
            {
                foreach (var rom in GenerateReferencedObjectMembers(type))
                {
                    rom.Parent = parentMember;
                    rom.Root = rootMember;

                    if (!rom.HasCycle)
                    {
                        members.Add(rom);
                        GenerateMembersImpl(rom.Type, members, rootMember, rom);
                    }
                }
            }

            if (HasChildCollectionMembers(type))
            {
                foreach (var ccp in GenerateChildCollectionMembers(type))
                {
                    ccp.Parent = parentMember;
                    ccp.Root = rootMember;

                    if (!ccp.HasCycle)
                    {
                        members.Add(ccp);
                        GenerateMembersImpl(ccp.CollectionType, members, rootMember, ccp);
                    }
                }
            }
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

                var mi = typeof(ModelCache).GetMethod("_runLazyChildCollectionQuery", BindingFlags.NonPublic | BindingFlags.Static);
                var genericMi = mi.MakeGenericMethod(cm.CollectionType);
                return genericMi.Invoke(null, new Object[] { queryParams }) as T;
            };

            return new Lazy<T>(func);
        }

        private static List<T> _runLazyChildCollectionQuery<T>(dynamic queryParams) where T : class, new()
        {
            return new Query<T>().Where(queryParams).Execute();
        }

        public static SetInstanceValuesDelegate GetSetInstanceValuesDelegate(IndexedSqlDataReader reader)
        {
            return _setInstanceValuesDelegateCache.GetOrAdd(reader.GetCacheId(), (id) =>
            {
                return GenerateSetInstanceValuesDelegate(reader);
            });
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
                    EmitInt32(il, index);
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
                    EmitInt32(il, index);

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
                        EmitInt32(il, index);
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
                    EmitInt32(il, index);

                    MethodInfo get_value = null;
                    if (type_accessors.ContainsKey(originType))
                        get_value = type_accessors[originType];
                    else if (origin == typeof(Guid))
                        get_value = get_guid_fn;
                    else
                        get_value = get_value_fn;

                    il.EmitCall(OpCodes.Callvirt, get_value, null);

                    if (originType != destinationType)
                    {
                        //we need to cast ...
                        if (destinationType == TypeCode.String)
                        {
                            var to_s = GetToStringForType(origin);
                            var local = il.DeclareLocal(origin);

                            il.Emit(OpCodes.Stloc, local.LocalIndex);
                            il.Emit(OpCodes.Ldloca_S, local.LocalIndex);
                            il.Emit(OpCodes.Call, to_s);
                        }
                        else
                            if (on_stack_conversions.ContainsKey(destinationType))
                                on_stack_conversions[destinationType]();
                            else
                                il.Emit(OpCodes.Newobj, c.Type);
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

        private static MethodInfo GetToStringForType(Type type)
        {
            var methods = type.GetMethods();
            return methods.First(m => m.Name == "ToString");
        }

        private static void EmitInt32(ILGenerator il, int value)
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
}