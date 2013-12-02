using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Destrier
{
    public static class ReflectionHelper
    {
        private static ConcurrentDictionary<Type, Dictionary<String, PropertyInfo>> _propertyCache = new ConcurrentDictionary<Type, Dictionary<String, PropertyInfo>>();

        private static ConcurrentDictionary<Type, Type[]> _interfaceCache = new ConcurrentDictionary<Type, Type[]>();
        private static ConcurrentDictionary<Type, Func<object>> _ctorCache = new ConcurrentDictionary<Type, Func<object>>();
        private static ConcurrentDictionary<PropertyInfo, Action<Object, Object>> _compiledSetFunctions = new ConcurrentDictionary<PropertyInfo, Action<object, object>>();

        private static ConcurrentDictionary<Type, Type> _nullableTypeCache = new ConcurrentDictionary<Type, Type>();
        private static ConcurrentDictionary<Type, Type> _collectionTypeCache = new ConcurrentDictionary<Type, Type>();
        private static ConcurrentDictionary<Type, bool> _isNullableTypeCache = new ConcurrentDictionary<Type, bool>();

        private static Func<Type, Func<object>> _ctorHelperFunc = ConstructorCreationHelper;

        public static Dictionary<String, PropertyInfo> GetProperties(Type type)
        {
            return _propertyCache.GetOrAdd(type, (t) =>
            {
                return type.GetProperties().ToDictionary(p => p.Name);
            });
        }


        public static object GetNewObject(Type toConstruct)
        {
            return _ctorCache.GetOrAdd(toConstruct, _ctorHelperFunc)();
        }

        public static T GetNewObject<T>()
        {
            var neededType = typeof(T);
            var ctor = _ctorCache.GetOrAdd(neededType, _ctorHelperFunc);

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
                return !(GetDefault(pi.PropertyType).Equals(pi.GetValue(target, null)));
            }

            object first = pi.GetValue(target, null);
            object second = GetDefault(pi.PropertyType);

            if (first != null && second != null)
            {
                return first.Equals(second);
            }
            return first != null;
        }

        public static Attribute GetAttribute(Type type, Type attributeType)
        {
            return type.GetCustomAttributes(attributeType, false).FirstOrDefault() as Attribute;
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
            return GetInterfaces(type).Any(i => i.Equals(interfaceType));
        }

        public static Boolean IsLazy(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Lazy<>);
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

        public static Type GetUnderlyingTypeForLazy(Type lazyType)
        {
            return lazyType.GetGenericArguments()[0];
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
                object second = GetDefault(type);

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

        public static Object ChangeType(Object value, Type destinationType)
        {
            return Convert.ChangeType(value, destinationType);
        }
    }
}
