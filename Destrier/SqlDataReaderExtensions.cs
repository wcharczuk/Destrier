using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;

namespace Destrier
{
    public static class SqlDataReaderExtensions
    {
        public static Dictionary<String, Int32> GetColumnMap(this SqlDataReader dr)
        {
            var hash = new Dictionary<String, Int32>();
            for (int i = 0; i < dr.FieldCount; i++)
            {
                var name = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToUpper(dr.GetName(i));
                if (!hash.ContainsKey(name))
                    hash.Add(name, i);
            }
            return hash;
        }

        public static string[] GetColumnIndexMap(this SqlDataReader dr)
        {
            String[] strings = new string[dr.FieldCount];
            for (int i = 0; i < dr.FieldCount; i++)
            {
                var name = ReflectionCache.StandarizeCasing(dr.GetName(i)); //System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToUpper(dr.GetName(i));
                strings[i] = name;
            }
            return strings;
        }

        public static dynamic ReadDynamic(this SqlDataReader dr)
        {
            var value = new AgileObject();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    for (int i = 0; i < dr.FieldCount; i++)
                    {
                        var name = dr.GetName(i);
                        ((IDictionary<string, object>)value)[name] = dr.GetValue(i);
                    }
                }
            }
            return value;
        }

        public static List<AgileObject> ReadDynamicList(this SqlDataReader dr)
        {
            List<AgileObject> values = new List<AgileObject>();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    dynamic value = new AgileObject();
                    for (int i = 0; i < dr.FieldCount; i++)
                    {
                        var name = dr.GetName(i);
                        ((IDictionary<string, object>)value)[name] = dr.GetValue(i);
                    }
                    values.Add(value);
                }
            }
            return values;
        }

        public static T ReadScalar<T>(this SqlDataReader dr, Boolean advanceToNextResultAfter = true) where T : struct
        {
            T value = default(T);

            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    if(!dr.IsDBNull(0))
                        value = (T)Convert.ChangeType(dr.GetValue(0), typeof(T));
                }
            }

            if (advanceToNextResultAfter)
                dr.NextResult();

            return value;
        }

        public static T ReadObject<T>(this SqlDataReader dr, Boolean returnNullOnEmpty = false, Boolean advanceToNextResultAfter = true) where T : class, IPopulate 
        {
            T newObject = ReflectionCache.GetNewObject<T>();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    newObject.Populate(dr);
                }
            }
            else if (returnNullOnEmpty)
            {
                newObject = null;
            }

            if (advanceToNextResultAfter)
                dr.NextResult();

            return newObject;
        }

        public static List<T> ReadScalarList<T>(this SqlDataReader dr, Boolean advanceToNextResultAfter = true)
        {
            List<T> list = new List<T>();

            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    list.Add((T)Convert.ChangeType(dr.GetValue(0), typeof(T)));
                }
            }

            if (advanceToNextResultAfter)
                dr.NextResult();

            return list;
        }

        public static List<T> ReadList<T>(this SqlDataReader dr, Boolean columnsCanBeMissing = false, Boolean advanceToNextResultAfter = true) where T : class, IPopulate
        {
            List<T> list = new List<T>();

            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    T newObject = ReflectionCache.GetNewObject<T>();
                    newObject.Populate(dr);
                    list.Add(newObject);
                }
            }

            if (advanceToNextResultAfter)
                dr.NextResult();

            return list;
        }

        public static Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(this SqlDataReader dr, Func<TValue, TKey> keySelector, Boolean advanceToNextResultAfter = true) where TValue : class, IPopulate
        {
            Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();

            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    TValue newObject = ReflectionCache.GetNewObject<TValue>();
                    newObject.Populate(dr);
                    TKey keyValue = keySelector(newObject);

                    if (!dict.ContainsKey(keyValue))
                        dict.Add(keyValue, newObject);
                }
            }

            if (advanceToNextResultAfter)
                dr.NextResult();

            return dict;
        }

        public static void ReadIntoParentCollection(this SqlDataReader dr, Type type, Action<SqlDataReader, IPopulate> doStuffToAddToParent, Boolean advanceToNextResultAfter = true, Boolean populateFullResults = false)
        {
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    IPopulate newObject = ReflectionCache.GetNewObject(type) as IPopulate;
                    if (newObject == null)
                        throw new Exception("type is not IPopulate");

                    if (populateFullResults && newObject is BaseModel)
                    {
                        ((BaseModel)newObject).PopulateFullResults(dr);
                    }
                    else
                        newObject.Populate(dr);

                    doStuffToAddToParent(dr, newObject);
                }
            }

            if (advanceToNextResultAfter)
                dr.NextResult();
        }

        public static void ReadIntoParentCollection<T>(this SqlDataReader dr, Action<SqlDataReader, T> doStuffToAddToParent, Boolean advanceToNextResultAfter = true) where T : class, IPopulate
        {
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    T newObject = ReflectionCache.GetNewObject<T>();
                    newObject.Populate(dr);
                    doStuffToAddToParent(dr, newObject);
                }
            }

            if (advanceToNextResultAfter)
                dr.NextResult();
        }

        public static void ReadFullControl(this SqlDataReader dr, Action<SqlDataReader> action, Boolean advanceToNextResultAfter = true)
        {
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    action(dr);
                }
            }

            if (advanceToNextResultAfter)
                dr.NextResult();
        }

        public static T Get<T>(this SqlDataReader dr, String columnName)
        {
            return (T)dr.Get(typeof(T), columnName);
        }

        public static object Get(this SqlDataReader dr, Type resultType, String columnName)
        {
            Boolean isNullableType = ReflectionCache.IsNullableType(resultType);
            Type effectiveType = isNullableType ? ReflectionCache.GetUnderlyingTypeForNullable(resultType) : resultType;

            var columns = dr.GetColumnMap();
            if (columns.ContainsKey(columnName))
            {
                int columnIndex = columns[columnName];

                if (!dr.IsDBNull(columnIndex))
                {
                    object value = dr[columnIndex];

                    if (effectiveType.IsEnum)
                        return Enum.ToObject(effectiveType, value);
                    else
                        return Convert.ChangeType(value, effectiveType);
                }
            }

            return isNullableType ? null : ReflectionCache.GetDefault(effectiveType);
        }
    }
}