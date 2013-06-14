using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.IO;
using System.Collections.Specialized;

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
            foreach(var prop in properties)            
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
            var neededType = toConstruct;
            var ctor = _ctorCache.GetOrAdd(neededType, _CtorHelperFunc);

            return ctor();
        }

        public static T GetNewObject<T>() where T : class
        {
            var neededType = typeof(T);
            var ctor = _ctorCache.GetOrAdd(neededType, _CtorHelperFunc);

            return ctor() as T;
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

        public static Boolean HasInterface<T, I>() where T : BaseModel
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
            if (value.GetType().Equals(destinationType))
                return value;
            else
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
    }
}