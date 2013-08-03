using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Collections.Concurrent;

namespace Destrier.Redis.Core
{
    public class ReflectionCache
    {
        private static ConcurrentDictionary<Type, List<Member>> _memberMapCache = new ConcurrentDictionary<Type, List<Member>>();

        public static List<Member> GetMemberMap(Type type)
        {
            return _memberMapCache.GetOrAdd(type, (t) => GenerateMemberMap(t).ToList());
        }

        public static IEnumerable<Member> GenerateMemberMap(Type t, Member parent = null)
        {
            foreach (var property in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsSerializableType(property.PropertyType))
                {
                    var propertyMember = new PropertyMember(property) { Parent = parent};
                    yield return propertyMember;

                    if (!property.PropertyType.IsValueType)
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

                    if (!field.FieldType.IsValueType)
                        foreach (var member in GenerateMemberMap(field.FieldType, parent: fieldMember))
                            yield return member;
                }
            }
        }

        public static Boolean IsSerializableType(Type t)
        {
            return 
                t.IsValueType 
                || t == typeof(String)
                || IsArrayType(t) 
                || IsListType(t) 
                || IsSetType(t) 
                || (IsMapType(t) && IsMapTypeKeyTypeSupported(t))
                || IsNullableType(t)
                || (t.IsClass && !IsEnumerableType(t));
        }

        public static Boolean IsEnumerableType(Type t)
        {
            return t.IsGenericType && t.GetInterfaces().Any(it => it == typeof(IEnumerable<>));
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

        //wctodo: re-write with lambda compilation.
        public static Func<Object, Object> CompileFieldAccess(FieldInfo field)
        {
            return (obj) => field.GetValue(obj);
        }

        //wctodo: re-write with lambda compilation.
        public static Action<Object, Object> CompileFieldAssignment(FieldInfo field)
        {
            return (obj, val) => field.SetValue(obj, val);
        }

        public static Func<Object, Object> CompilePropertyAccess(PropertyInfo property)
        {
            var instanceParameter = Expression.Parameter(typeof(object), "instance");
            var lambda = Expression.Lambda<Func<object, object>>
            (
                Expression.MakeMemberAccess
                (
                    Expression.Property(Expression.Convert(instanceParameter, property.DeclaringType), property)
                    , property
                )
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
