using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Destrier
{
    public class MonkeyPatch
    {
        public static void SetProperty<T>(object instance, String propertyName, object value)
        {
            var properties = typeof(T).GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (property.Name.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase))
                {
                    property.SetValue(instance, value);
                }
            }
        }

        public static void SetStaticProperty<T>(String propertyName, object value)
        {
            var properties = typeof(T).GetProperties(BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var property in properties)
            {
                if (property.Name.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase))
                {
                    property.SetValue(null, value);
                }
            }
        }

        public static void SetStaticField<T>(String fieldName, object value)
        {
            var fields = typeof(T).GetFields(BindingFlags.NonPublic | BindingFlags.Static);

            foreach (var field in fields)
            {
                if (field.Name.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase))
                {
                    field.SetValue(null, value);
                }
            }
        }

        public static void SetField<T>(object instance, String fieldName, object value)
        {
            var fields = typeof(T).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.Name.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase))
                {
                    field.SetValue(instance, value);
                }
            }
        }

        public static F GetField<T, F>(T instance, String fieldName)
        {
            var fields = typeof(T).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.Name.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return (F)field.GetValue(instance);
                }
            }
            return default(F);
        }

        public static F GetProperty<T, F>(T instance, String fieldName)
        {
            var fields = typeof(T).GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.Name.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return (F)field.GetValue(instance);
                }
            }
            return default(F);
        }

        public static F GetStaticField<T, F>(String fieldName)
        {
            var fields = typeof(T).GetFields(BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var field in fields)
            {
                if (field.Name.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return (F)field.GetValue(null);
                }
            }
            return default(F);
        }

        public static F GetStaticProperty<T, F>(String fieldName)
        {
            var fields = typeof(T).GetProperties(BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var field in fields)
            {
                if (field.Name.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return (F)field.GetValue(null);
                }
            }
            return default(F);
        }
    }
}
