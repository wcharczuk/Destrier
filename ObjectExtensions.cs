using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;

namespace Destrier
{
    public static class ObjectExtensions
    {
        public static object DBNullCoalese(this Object obj)
        {            
            if (obj == null)
                return DBNull.Value;

            if (obj is String && String.IsNullOrEmpty(obj.ToString()))
                return DBNull.Value;
            
            if (obj is DateTime && obj.Equals(default(DateTime)))
                return DBNull.Value;

            return obj;
        }

        public static String DisplayIfNull(this Object obj, String text)
        {
            if (obj == null)
                return text;

            Type t = obj.GetType();
            var defaultValue = ReflectionCache.GetDefault(t);

            if (obj.Equals(defaultValue))
                return text;

            return obj.ToString();
        }

        /// <summary>
        /// Read the properties from the target object, setting properties on the host object where the names (and types) match.
        /// </summary>
        /// <param name="mapTo"></param>
        /// <param name="mapFrom"></param>
        public static void SetPropertiesFrom(this Object mapTo, Object mapFrom)
        {
            if (mapTo == null || mapFrom == null)
                return;

            //build a property name dictionary for mapTo
            var toProps = mapTo.GetType().GetProperties().ToDictionary(prop => prop.Name);

            //build a property name dictionary for mapFrom
            var fromProps = mapFrom.GetType().GetProperties().ToDictionary(prop => prop.Name);

            //iterate over mapFrom names, see if they exist in mapTo, see if mapTo supports set, set.
            foreach (var propertyName in fromProps.Keys)
            {
                if (toProps.ContainsKey(propertyName))
                {
                    var fromProp = fromProps[propertyName];
                    var toProp = toProps[propertyName];

                    if (toProp.CanWrite && toProp.PropertyType.IsAssignableFrom(fromProp.PropertyType))
                    {
                        var value = fromProp.GetValue(mapFrom, null);
                        toProp.SetValue(mapTo, value, null);
                    }
                }
            }
        }
    }
}
