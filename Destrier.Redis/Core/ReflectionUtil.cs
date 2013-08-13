using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Collections.Concurrent;
using System.IO;

namespace Destrier.Redis.Core
{
    public class ReflectionUtil
    {
        private static ConcurrentDictionary<Type, Func<object>> _ctorCache = new ConcurrentDictionary<Type, Func<object>>();
        private static ConcurrentDictionary<Type, List<Member>> _memberMapCache = new ConcurrentDictionary<Type, List<Member>>();

        private static Func<Type, Func<object>> _CtorHelperFunc = ConstructorCreationHelper;

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

        public static List<Member> GetMemberMap(Type type)
        {
            return _memberMapCache.GetOrAdd(type, (t) => GenerateMemberMap(t).ToList());
        }

        public static IEnumerable<Member> GenerateMemberMap(Type t, Member parent = null, Boolean recursive = true)
        {
            foreach (var property in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsSerializableType(property.PropertyType))
                {
                    var propertyMember = new PropertyMember(property) { Parent = parent};
                    yield return propertyMember;

                    if (IsTraversableType(property.PropertyType) && recursive)
                        foreach (var member in GenerateMemberMap(property.PropertyType, parent: propertyMember))
                            yield return member;
                }
            }

            foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsSerializableType(field.FieldType))
                {
                    var fieldMember = new FieldMember(field) { Parent = parent };
                    yield return fieldMember;

                    if (IsTraversableType(field.FieldType) && recursive)
                        foreach (var member in GenerateMemberMap(field.FieldType, parent: fieldMember))
                            yield return member;
                }
            }
        }

        public static void MapToDictionary(Object instance, IDictionary<Member, Object> mappedMembers, Member parent = null)
        {
            var t = instance.GetType();
            foreach (var property in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsSerializableType(property.PropertyType))
                {
                    var member = new PropertyMember(property) { Parent = parent };
                    var value = member.GetValue(instance);

                    mappedMembers.Add(member, value);

                    if (IsTraversableType(member.MemberType) && !member.IsBinarySerialized)
                        MapToDictionary(value, mappedMembers, member);
                }
            }

            foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsSerializableType(field.FieldType))
                {
                    var member = new FieldMember(field) { Parent = parent };
                    var value = member.GetValue(instance);

                    mappedMembers.Add(member, value);

                    if (IsTraversableType(member.MemberType) && !member.IsBinarySerialized)
                        MapToDictionary(value, mappedMembers, member);
                }
            }
        }

        public static Boolean IsSerializableType(Type t)
        {
            return 
                t.IsValueType 
                || t == typeof(String)
                || IsNullableType(t)
                || (t.IsClass && !IsEnumerableType(t));
        }

        public static Boolean IsTraversableType(Type t)
        {
            return 
                (
                    t.IsClass 
                    && !IsEnumerableType(t) 
                    && t != typeof(String)
                )
                || (IsNullableType(t) && IsTraversableType(GetUnderlyingTypeForNullable(t)));
        }

        public static Boolean IsEnumerableType(Type t)
        {
            return t.IsGenericType && t.GetInterfaces().Any(it => it == typeof(IEnumerable<>) || it == typeof(System.Collections.IEnumerable));
        }

        public static Boolean IsArrayType(Type t)
        {
            return t.IsArray;
        }

        public static Boolean IsListType(Type t)
        {
            return t.IsGenericType && t.GetInterfaces().Any(it => it == typeof(IList<>));
        }

        public static Boolean IsSetType(Type t)
        {
            return t.IsGenericType && t.GetInterfaces().Any(it => it == typeof(ISet<>));
        }

        public static Boolean IsMapType(Type t)
        {
            return t.IsGenericType && t.GetInterfaces().Any(it => it == typeof(IDictionary<,>));
        }

        public static Boolean IsMapTypeKeyTypeSupported(Type t)
        {
            return t.IsGenericType && (
                t.GetGenericArguments()[0].IsValueType 
                || t.GetGenericArguments()[0] == typeof(string)
                || t.GetGenericArguments()[0].IsEnum
            );
        }

        public static Boolean IsNullableType(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static Type GetUnderlyingTypeForNullable(Type t)
        {
            System.ComponentModel.NullableConverter nullableConverter = new System.ComponentModel.NullableConverter(t);
            return nullableConverter.UnderlyingType;
        }

        public static Func<Object, Object> CompileFieldAccess(FieldInfo field)
        {
            return (obj) => field.GetValue(obj);
        }

        public static Action<Object, Object> CompileFieldAssignment(FieldInfo field)
        {
            return (obj, val) => field.SetValue(obj, val);
        }

        public static Func<Object, Object> CompilePropertyAccess(PropertyInfo property)
        {
            var instanceParameter = Expression.Parameter(typeof(object), "instance");
            var lambda = Expression.Lambda<Func<object, object>>
            (
                Expression.Convert(Expression.MakeMemberAccess(Expression.Convert(instanceParameter, property.DeclaringType), property), typeof(object))
                , instanceParameter
            );

            return lambda.Compile();
        }

        public static Action<Object, Object> CompilePropertyAssignment(PropertyInfo property)
        {
            var instanceParameter = Expression.Parameter(typeof(object), "instance");
            var valueParameter = Expression.Parameter(typeof(object), "value");
            var lambda = Expression.Lambda<Action<object, object>>
            (
                Expression.Assign
                (
                    Expression.Property(Expression.Convert(instanceParameter, property.DeclaringType), property)
                    , Expression.Convert(valueParameter, property.PropertyType)
                )
                , instanceParameter
                , valueParameter
            );

            return lambda.Compile();
        }
    }
}
