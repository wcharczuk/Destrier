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
        private static ConcurrentDictionary<Type, Member[]> _memberCache = new ConcurrentDictionary<Type, Member[]>();
        private static ConcurrentDictionary<Type, ColumnMember[]> _columnMemberCache = new ConcurrentDictionary<Type, ColumnMember[]>();

        private static ConcurrentDictionary<Type, Dictionary<String, ColumnMember>> _columnMemberLookupStandardizedCache = new ConcurrentDictionary<Type, Dictionary<String, ColumnMember>>();
        private static ConcurrentDictionary<Type, Dictionary<String, ColumnMember>> _columnMemberLookupCache = new ConcurrentDictionary<Type, Dictionary<String, ColumnMember>>();

        private static ConcurrentDictionary<Type, ReferencedObjectMember[]> _referencedObjectMemberCache = new ConcurrentDictionary<Type, ReferencedObjectMember[]>();
        private static ConcurrentDictionary<Type, ChildCollectionMember[]> _childCollectionMemberCache = new ConcurrentDictionary<Type, ChildCollectionMember[]>();

        private static ConcurrentDictionary<Type, List<Member>> _recursiveMemberCache = new ConcurrentDictionary<Type, List<Member>>();
        private static ConcurrentDictionary<Type, RootMember> _rootMemberCache = new ConcurrentDictionary<Type, RootMember>();

        private static ConcurrentDictionary<Type, TableAttribute> _tableAttributeCache = new ConcurrentDictionary<Type, TableAttribute>();
        private static ConcurrentDictionary<Type, bool> _hasChildCollectionPropertiesCache = new ConcurrentDictionary<Type, bool>();
        private static ConcurrentDictionary<Type, bool> _hasReferencedObjectPropertiesCache = new ConcurrentDictionary<Type, bool>();

        private static ConcurrentDictionary<Type, PropertyInfo[]> _columnMemberPropertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static ConcurrentDictionary<Type, PropertyInfo[]> _referencedObjectMemberPropertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static ConcurrentDictionary<Type, PropertyInfo[]> _childCollectionMemberPropertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static ConcurrentDictionary<String, SetInstanceValuesDelegate> _setInstanceValuesDelegateCache = new ConcurrentDictionary<String, SetInstanceValuesDelegate>();

        public static Dictionary<String, ColumnMember> GetColumnMemberStandardizedLookup(Type type)
        {
            return _columnMemberLookupStandardizedCache.GetOrAdd(type, (t) =>
            {
                return Model.GenerateColumnMemberStandardizedLookup(t);
            });
        }

        public static Dictionary<String, ColumnMember> GetColumnMemberLookup(Type type)
        {
            return _columnMemberLookupCache.GetOrAdd(type, (t) =>
            {
                return Model.GenerateColumnMembers(t).ToDictionary(cm => cm.Name);
            });
        }

        public static ColumnMember[] GetColumnMembers(Type type)
        {
            return _columnMemberCache.GetOrAdd(type, (t) =>
            {
                return Model.GenerateColumnMembers(t);
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

        public static ReferencedObjectMember[] GetReferencedObjectMembers(Type type)
        {
            return _referencedObjectMemberCache.GetOrAdd(type, (t) =>
            {
                return Model.GenerateReferencedObjectMembers(t);
            });
        }

        public static ChildCollectionMember[] GetChildCollectionMembers(Type type)
        {
            return _childCollectionMemberCache.GetOrAdd(type, (t) =>
            {
                return Model.GenerateChildCollectionMembers(t);
            });
        }

        public static Boolean HasReferencedObjectMembers(Type type)
        {
            return _hasReferencedObjectPropertiesCache.GetOrAdd(type, (t) =>
            {
                return Model.GenerateReferencedObjectMembers(t).Any();
            });
        }

        public static Boolean HasChildCollectionMembers(Type type)
        {
            return _hasChildCollectionPropertiesCache.GetOrAdd(type, (t) =>
            {
                return Model.GenerateChildCollectionMembers(t).Any();
            });
        }

        public static TableAttribute GetTableAttribute(Type type)
        {
            return _tableAttributeCache.GetOrAdd(type, (t) =>
            {
                return ModelReflection.TableAttribute(t);
            });
        }

        public static RootMember GetRootMemberForType(Type type)
        {
            return _rootMemberCache.GetOrAdd(type, (t) =>
            {
                return Model.GenerateRootMemberForType(t);
            });
        }

        public static List<Member> AllMembers(Type type)
        {
            return _recursiveMemberCache.GetOrAdd(type, (t) =>
            {
                return Model.GenerateAllMembers(t);
            });
        }

        public static SetInstanceValuesDelegate GetSetInstanceValuesDelegate(IndexedSqlDataReader reader)
        {
            return _setInstanceValuesDelegateCache.GetOrAdd(reader.GetCacheId(), (id) =>
            {
                return Model.GenerateSetInstanceValuesDelegate(reader);
            });
        }
    }
}