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
    public class ReflectionCache
    {
        //these i have to keep
        private static ConcurrentDictionary<Type, Type[]> _interfaceCache = new ConcurrentDictionary<Type, Type[]>();
        private static ConcurrentDictionary<Type, Func<object>> _ctorCache = new ConcurrentDictionary<Type, Func<object>>();
        private static ConcurrentDictionary<PropertyInfo, Action<Object, Object>> _compiledSetFunctions = new ConcurrentDictionary<PropertyInfo, Action<object, object>>();

        private static ConcurrentDictionary<Type, List<Member>> _memberCache = new ConcurrentDictionary<Type, List<Member>>();
        private static ConcurrentDictionary<Type, List<ColumnMember>> _columnMemberCache = new ConcurrentDictionary<Type, List<ColumnMember>>();

        private static ConcurrentDictionary<Type, Dictionary<String, ColumnMember>> _columnMemberLookupStandardizedCache = new ConcurrentDictionary<Type, Dictionary<String, ColumnMember>>();
        private static ConcurrentDictionary<Type, Dictionary<String, ColumnMember>> _columnMemberLookupCache = new ConcurrentDictionary<Type, Dictionary<String, ColumnMember>>();

        private static ConcurrentDictionary<Type, List<ReferencedObjectMember>> _referencedObjectMemberCache = new ConcurrentDictionary<Type, List<ReferencedObjectMember>>();
        private static ConcurrentDictionary<Type, List<ChildCollectionMember>> _childCollectionMemberCache = new ConcurrentDictionary<Type, List<ChildCollectionMember>>();

        private static ConcurrentDictionary<Type, List<Member>> _recursiveMemberCache = new ConcurrentDictionary<Type, List<Member>>();
        private static ConcurrentDictionary<Type, RootMember> _rootMemberCache = new ConcurrentDictionary<Type, RootMember>();

        private static ConcurrentDictionary<Type, TableAttribute> _tableAttributeCache = new ConcurrentDictionary<Type, TableAttribute>();
        private static ConcurrentDictionary<Type, Type> _nullableTypeCache = new ConcurrentDictionary<Type, Type>();
        private static ConcurrentDictionary<Type, Type> _collectionTypeCache = new ConcurrentDictionary<Type, Type>();
        private static ConcurrentDictionary<Type, bool> _isNullableTypeCache = new ConcurrentDictionary<Type, bool>();
        private static ConcurrentDictionary<Type, bool> _hasChildCollectionPropertiesCache = new ConcurrentDictionary<Type, bool>();
        private static ConcurrentDictionary<Type, bool> _hasReferencedObjectPropertiesCache = new ConcurrentDictionary<Type, bool>();

        private static ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static ConcurrentDictionary<Type, PropertyInfo[]> _columnMemberPropertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static ConcurrentDictionary<Type, PropertyInfo[]> _referencedObjectMemberPropertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static ConcurrentDictionary<Type, PropertyInfo[]> _childCollectionMemberPropertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();

        private static Func<Type, Func<object>> _CtorHelperFunc = ConstructorCreationHelper;

        public static PropertyInfo[] GetProperties(Type type)
        {
            return _propertyCache.GetOrAdd(type, (t) =>
            {
                return type.GetProperties();
            });
        }

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
                PropertyInfo[] properties = GetProperties(type);
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
                PropertyInfo[] properties = GetProperties(type);
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
                PropertyInfo[] properties = GetProperties(type);
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

        public static object GetNewObject(Type toConstruct)
        {
            return _ctorCache.GetOrAdd(toConstruct, _CtorHelperFunc)();
        }

        public static T GetNewObject<T>()
        {
            var neededType = typeof(T);
            var ctor = _ctorCache.GetOrAdd(neededType, _CtorHelperFunc);

            return (T)ctor();
        }

        public static Func<object> ConstructorCreationHelper(Type target)
        {
            return Expression.Lambda<Func<object>>(Expression.New(target)).Compile();
        }

        public static Func<T> ConstructorCreationHelper<T>()
        {
            return Expression.Lambda<Func<T>>(Expression.New(typeof(T))).Compile();
        }

        public static Func<T> ConstructorCreationHelper<T>(Type target)
        {
            return Expression.Lambda<Func<T>>(Expression.New(target)).Compile();
        }

        public static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        public static object GetValueOfPropertyForObject(object target, string propertyName, Boolean ignoreCasing = true)
        {
            var propList = target.GetType().GetProperties();
            return propList.FirstOrDefault(p => p.Name.Equals(propertyName, ignoreCasing ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture)).GetValue(target, null);
        }

        public static Boolean IsSet(PropertyInfo pi, object target)
        {
            if (pi.PropertyType.IsValueType)
            {
                return !(ReflectionCache.GetDefault(pi.PropertyType).Equals(pi.GetValue(target, null)));
            }

            object first = pi.GetValue(target, null);
            object second = ReflectionCache.GetDefault(pi.PropertyType);

            if (first != null && second != null)
            {
                return first.Equals(second);
            }
            return first != null;
        }

        public static Type[] GetInterfaces(Type type)
        {
            return _interfaceCache.GetOrAdd(type, type.GetInterfaces());
        }

        public static Boolean HasInterface<T, I>()
        {
            return HasInterface(typeof(T), typeof(I));
        }

        public static Boolean HasInterface(Type type, Type interfaceType)
        {
            return ReflectionCache.GetInterfaces(type).Any(i => i.Equals(interfaceType));
        }

        public static Boolean IsNullableType(Type type)
        {
            return _isNullableTypeCache.GetOrAdd(type, (t) =>
            {
                return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
            });
        }

        public static Type GetUnderlyingTypeForNullable(Type nullableType)
        {
            return _nullableTypeCache.GetOrAdd(nullableType, (nt) =>
            {
                System.ComponentModel.NullableConverter nullableConverter = new System.ComponentModel.NullableConverter(nt);
                return nullableConverter.UnderlyingType;
            });
        }

        public static Type GetUnderlyingTypeForCollection(Type collectionType)
        {
            return _collectionTypeCache.GetOrAdd(collectionType, (ct) =>
            {
                return ct.GetGenericArguments()[0];
            });
        }

        public static Boolean IsSet(object value)
        {
            if (value == null)
                return false;

            return IsSet(value.GetType(), value);
        }

        public static Boolean IsSet(Type type, object value)
        {
            if (type.IsValueType && !IsNullableType(type))
                return true;
            else
            {
                if (value == null)
                    return false;

                object first = value;
                object second = ReflectionCache.GetDefault(type);

                if (first != null && second != null)
                {
                    return first.Equals(second);
                }
                return first != null;
            }
        }

        public static Action<Object, Object> GetSetAction(PropertyInfo property)
        {
            return _compiledSetFunctions.GetOrAdd(property, (p) =>
            {
                return GenerateSetAction(p);
            });
        }

        public static Action<Object, Object> GenerateSetAction(PropertyInfo property)
        {
            var instanceParameter = Expression.Parameter(typeof(object), "instance");
            var valueParameter = Expression.Parameter(typeof(object), "value");
            var lambda = Expression.Lambda<Action<object, object>>(
                Expression.Assign(
                    Expression.Property(Expression.Convert(instanceParameter, property.DeclaringType), property),
                    Expression.Convert(valueParameter, property.PropertyType)),
                instanceParameter,
                valueParameter
            );

            return lambda.Compile();
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

        public static Object ChangeType(Object value, Type destinationType)
        {
            return Convert.ChangeType(value, destinationType);
        }

        #endregion

        public static RootMember GetRootMemberForType(Type type)
        {
            return _rootMemberCache.GetOrAdd(type, (t) =>
            {
                return new RootMember(t) { TableAlias = Model.GenerateAlias(), OutputTableName = Model.GenerateAlias() };
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

        public delegate void SetInstanceValuesDelegate(IndexedSqlDataReader dr, object instance);

        public static SetInstanceValuesDelegate GenerateSetInstanceValuesDelegate(IndexedSqlDataReader dr)
        {
            Type idr = dr.GetType();
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
                    var underlyingType = c.NullableUnderlyingType;

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

                    if (c.ColumnAttribute.CanBeNull)
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