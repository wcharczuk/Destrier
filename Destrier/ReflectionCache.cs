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
    /// Set of utility functions used for reflection heavy operations in the content cache.
    /// </summary>
    public class ReflectionCache
    {
        private static ConcurrentDictionary<String, Type> _typeNameCache = new ConcurrentDictionary<string, Type>();
        private static ConcurrentDictionary<Type, Type[]> _interfaceCache = new ConcurrentDictionary<Type, Type[]>();
        private static ConcurrentDictionary<Type, Func<object>> _ctorCache = new ConcurrentDictionary<Type, Func<object>>();
        private static ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static ConcurrentDictionary<Type, PropertyInfo[]> _columnCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static ConcurrentDictionary<Type, Dictionary<String, ColumnMember>> _columnMemberCache = new ConcurrentDictionary<Type, Dictionary<String, ColumnMember>>();
        private static ConcurrentDictionary<Type, Dictionary<String, ColumnMember>> _columnMemberStandardizedCache = new ConcurrentDictionary<Type, Dictionary<String, ColumnMember>>();
        private static ConcurrentDictionary<Type, PropertyInfo[]> _columnsPrimaryKeyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static ConcurrentDictionary<Type, PropertyInfo[]> _columnsNonPrimaryKeyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static ConcurrentDictionary<Type, ColumnAttribute[]> _columnAttributeCache = new ConcurrentDictionary<Type, ColumnAttribute[]>();
        private static ConcurrentDictionary<Type, PropertyInfo[]> _referencedObjectCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static ConcurrentDictionary<Type, PropertyInfo[]> _childCollectionCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static ConcurrentDictionary<Type, TableAttribute> _tableAttributeCache = new ConcurrentDictionary<Type, TableAttribute>();
        private static ConcurrentDictionary<PropertyInfo, ColumnAttribute> _columnAttributePropertyCache = new ConcurrentDictionary<PropertyInfo, ColumnAttribute>();
        private static ConcurrentDictionary<PropertyInfo, ReferencedObjectAttribute> _referencedObjectAttributeCache = new ConcurrentDictionary<PropertyInfo, ReferencedObjectAttribute>();
        private static ConcurrentDictionary<PropertyInfo, ChildCollectionAttribute> _childCollectionAttributeCache = new ConcurrentDictionary<PropertyInfo, ChildCollectionAttribute>();
        private static ConcurrentDictionary<Type, Type> _nullableTypeCache = new ConcurrentDictionary<Type, Type>();
        private static ConcurrentDictionary<Type, Type> _collectionTypeCache = new ConcurrentDictionary<Type, Type>();
        private static ConcurrentDictionary<Type, bool> _hasChildCollectionPropertiesCache = new ConcurrentDictionary<Type, bool>();
        private static ConcurrentDictionary<Type, bool> _hasReferencedObjectPropertiesCache = new ConcurrentDictionary<Type, bool>();
        private static ConcurrentDictionary<PropertyInfo, Action<Object, Object>> _compiledSetFunctions = new ConcurrentDictionary<PropertyInfo, Action<object, object>>();
        private static ConcurrentDictionary<Type, List<Member>> _recursiveMemberCache = new ConcurrentDictionary<Type, List<Member>>();
        private static ConcurrentDictionary<Type, RootMember> _rootMemberCache = new ConcurrentDictionary<Type, RootMember>();

        private static Func<Type, Func<object>> _CtorHelperFunc = ConstructorCreationHelper;

        public static Type GetTypeFromName(String fullTypeName)
        {
            return _typeNameCache.GetOrAdd(fullTypeName, Type.GetType(fullTypeName));
        }

        public static PropertyInfo[] GetProperties(Type type)
        {
            return _propertyCache.GetOrAdd(type, type.GetProperties());
        }

        public static Type[] GetInterfaces(Type type)
        {
            return _interfaceCache.GetOrAdd(type, type.GetInterfaces());
        }

        public static PropertyInfo[] GetColumns(Type type)
        {
            return _columnCache.GetOrAdd(type, (t) =>
            {
                List<PropertyInfo> columnProperties = GetNewObject<List<PropertyInfo>>();

                PropertyInfo[] properties = ReflectionCache.GetProperties(t);

                foreach (PropertyInfo pi in properties)
                {
                    object[] attributes = pi.GetCustomAttributes(typeof(ColumnAttribute), false);
                    if (attributes.Any())
                    {
                        columnProperties.Add(pi);
                    }
                }

                return columnProperties.ToArray();
            });
        }

        public static Dictionary<String, ColumnMember> GetColumnMembersStandardized(Type type)
        {
            return _columnMemberStandardizedCache.GetOrAdd(type, (t) =>
            {
                PropertyInfo[] columnProperties = GetColumns(type);
                var members = new List<ColumnMember>();
                foreach (var prop in columnProperties)
                {
                    members.Add(new ColumnMember(prop));
                }

                return members.ToDictionary(cm => Model.StandardizeCasing(cm.Name));
            });
        }

        public static Dictionary<String, ColumnMember> GetColumnMembers(Type type)
        {
            return _columnMemberCache.GetOrAdd(type, (t) =>
            {
                PropertyInfo[] columnProperties = GetColumns(type);
                var members = new List<ColumnMember>();
                foreach (var prop in columnProperties)
                {
                    members.Add(new ColumnMember(prop));
                }

                return members.ToDictionary(cm => cm.Name);
            });
        }

        public static Boolean HasReferencedObjectProperties(Type type)
        {
            return _hasReferencedObjectPropertiesCache.GetOrAdd(type, (t) =>
            {
                return GetReferencedObjectProperties(t).Any();
            });
        }

        public static PropertyInfo[] GetReferencedObjectProperties(Type type)
        {
            return _referencedObjectCache.GetOrAdd(type, (t) =>
            {
                List<PropertyInfo> referencedObjectProperties = GetNewObject<List<PropertyInfo>>();

                PropertyInfo[] properties = ReflectionCache.GetProperties(t);
                foreach (PropertyInfo pi in properties)
                {
                    object[] attributes = pi.GetCustomAttributes(typeof(ReferencedObjectAttribute), false);
                    if (attributes.Any())
                    {
                        referencedObjectProperties.Add(pi);
                    }
                }

                return referencedObjectProperties.ToArray();
            });
        }

        public static Boolean HasChildCollectionProperties(Type type)
        {
            return _hasChildCollectionPropertiesCache.GetOrAdd(type, (t) =>
            {
                return GetChildCollectionProperties(t).Any();
            });
        }

        public static PropertyInfo[] GetChildCollectionProperties(Type type)
        {
            return _childCollectionCache.GetOrAdd(type, (t) =>
            {
                List<PropertyInfo> childCollectionProperties = GetNewObject<List<PropertyInfo>>();

                PropertyInfo[] properties = ReflectionCache.GetProperties(t);
                foreach (PropertyInfo pi in properties)
                {
                    object[] attributes = pi.GetCustomAttributes(typeof(ChildCollectionAttribute), false);
                    if (attributes.Any())
                    {
                        childCollectionProperties.Add(pi);
                    }
                }

                return childCollectionProperties.ToArray();
            });
        }

        public static ColumnAttribute GetColumnAttribute(PropertyInfo property)
        {
            return _columnAttributePropertyCache.GetOrAdd(property, (pi) =>
            {
                return pi.GetCustomAttributes(typeof(ColumnAttribute), false).FirstOrDefault() as ColumnAttribute;
            });
        }

        public static ColumnAttribute[] GetColumnAttributes(Type type)
        {
            return _columnAttributeCache.GetOrAdd(type, (t) =>
            {
                var columnAttributes = GetNewObject<List<ColumnAttribute>>();
                var properties = ReflectionCache.GetProperties(t);

                foreach (PropertyInfo pi in properties)
                {
                    columnAttributes.Add(pi.GetCustomAttributes(typeof(ColumnAttribute), false).FirstOrDefault() as ColumnAttribute);
                }

                return columnAttributes.ToArray();
            });
        }

        public static TableAttribute GetTableAttribute(Type type)
        {
            return _tableAttributeCache.GetOrAdd(type, (t) =>
            {
                return t.GetCustomAttributes(true).FirstOrDefault(at => at is TableAttribute) as TableAttribute;
            });
        }

        public static ReferencedObjectAttribute GetReferencedObjectAttribute(PropertyInfo property)
        {
            return _referencedObjectAttributeCache.GetOrAdd(property, (pi) =>
            {
                return pi.GetCustomAttributes(typeof(ReferencedObjectAttribute), false).FirstOrDefault() as ReferencedObjectAttribute;
            });
        }

        public static ChildCollectionAttribute GetChildCollectionAttribute(PropertyInfo property)
        {
            return _childCollectionAttributeCache.GetOrAdd(property, (pi) =>
            {
                return pi.GetCustomAttributes(typeof(ChildCollectionAttribute), false).FirstOrDefault() as ChildCollectionAttribute;
            });
        }

        public static PropertyInfo[] GetColumnsPrimaryKey(Type type)
        {
            return _columnsPrimaryKeyCache.GetOrAdd(type, (t) =>
            {
                List<PropertyInfo> primaryKeys = GetNewObject<List<PropertyInfo>>();

                foreach (PropertyInfo pi in GetColumns(t))
                {
                    ColumnAttribute ca = GetColumnAttribute(pi);
                    if (ca.IsPrimaryKey)
                    {
                        primaryKeys.Add(pi);
                    }
                }

                return primaryKeys.ToArray();
            });
        }

        public static PropertyInfo[] GetColumnsNonPrimaryKey(Type type)
        {
            return _columnsNonPrimaryKeyCache.GetOrAdd(type, (t) =>
            {
                List<PropertyInfo> nonPrimaryKeys = GetNewObject<List<PropertyInfo>>();

                foreach (PropertyInfo pi in GetColumns(t))
                {
                    ColumnAttribute ca = GetColumnAttribute(pi);
                    if (!ca.IsPrimaryKey)
                    {
                        nonPrimaryKeys.Add(pi);
                    }
                }

                return nonPrimaryKeys.ToArray();
            });
        }

        public static object GetNewObject(String fullTypeName)
        {
            var neededType = GetTypeFromName(fullTypeName);
            var ctor = _ctorCache.GetOrAdd(neededType, _CtorHelperFunc);
            return ctor();
        }

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

        public static String GetFullTypeName(Type target)
        {
            String typeName = target.FullName;
            String assemblyName = target.Assembly.FullName.Split(',')[0];
            return String.Format("{0}, {1}", typeName, assemblyName);
        }

        public static Byte[] GetBytesForObject(Object o)
        {
            MemoryStream ms = new MemoryStream();
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            bf.Serialize(ms, o);
            return ms.ToArray();
        }

        public static Object GetObjectFromBytes(Type t, Byte[] bytes)
        {
            MemoryStream ms = new MemoryStream(bytes);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            return bf.Deserialize(ms);
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
            var propList = ReflectionCache.GetProperties(target.GetType());
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
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
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

        public static List<Member> Members(Type type, Member rootMember = null, Member parentMember = null)
        {
            List<Member> members = new List<Member>();
            foreach (var cpi in GetColumns(type))
            {
                var columnMember = new ColumnMember(cpi) { Parent = parentMember, Root = rootMember };
                if (!columnMember.Skip)
                    members.Add(columnMember);
            }
            if (GetReferencedObjectProperties(type).Any())
            {
                foreach (var referencedObjectProperty in GetReferencedObjectProperties(type))
                {
                    var referencedObjectMember = new ReferencedObjectMember(referencedObjectProperty) { Parent = parentMember, Root = rootMember };
                    members.Add(referencedObjectMember);
                }
            }
            if (GetChildCollectionProperties(type).Any())
            {
                foreach (var childCollectionProperty in GetChildCollectionProperties(type))
                {
                    var childCollectionMember = new ChildCollectionMember(childCollectionProperty) { Parent = parentMember, Root = rootMember };
                    members.Add(childCollectionMember);
                }
            }
            return members;
        }

        public static List<Member> MembersRecursiveCached(Type type)
        {
            return _recursiveMemberCache.GetOrAdd(type, (t) =>
            {
                return MembersRecursive(t);
            });
        }

        public static List<Member> MembersRecursive(Type type, Member rootMember = null, Member parent = null)
        {
            var members = new List<Member>();
            rootMember = rootMember ?? GetRootMemberForType(type);
            MembersImpl(type, members, rootMember, parent);
            return members.ToList();
        }

        public static RootMember GetRootMemberForType(Type type)
        {
            return _rootMemberCache.GetOrAdd(type, (t) =>
            {
                return new RootMember(t) { TableAlias = Model.GenerateAlias(), OutputTableName = Model.GenerateAlias() };
            });
        }
        private static void MembersImpl(Type type, List<Member> members, Member rootMember, Member parentMember = null)
        {
            foreach (var cpi in GetColumns(type))
            {
                var columnMember = new ColumnMember(cpi) { Parent = parentMember, Root = rootMember };
                if (!columnMember.Skip)
                    members.Add(columnMember);
            }

            if (HasReferencedObjectProperties(type))
            {
                foreach (var referencedObjectProperty in GetReferencedObjectProperties(type))
                {
                    var referencedObjectMember = new ReferencedObjectMember(referencedObjectProperty) { Parent = parentMember, Root = rootMember };

                    if (!referencedObjectMember.HasCycle)
                    {
                        members.Add(referencedObjectMember);
                        MembersImpl(referencedObjectMember.Type, members, rootMember, referencedObjectMember);
                    }
                }
            }

            if (HasChildCollectionProperties(type))
            {
                foreach (var childCollectionProperty in GetChildCollectionProperties(type))
                {
                    var childCollectionMember = new ChildCollectionMember(childCollectionProperty) { Parent = parentMember, Root = rootMember };

                    if (!childCollectionMember.HasCycle)
                    {
                        members.Add(childCollectionMember);
                        MembersImpl(childCollectionMember.CollectionType, members, rootMember, childCollectionMember);
                    }
                }
            }
        }
    }
}