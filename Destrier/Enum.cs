using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier
{
    public class EnumUtil
    {
        public static IEnumerable<EnumValue<T>> Explode<T>()
        {
            Type enumType = typeof(T);
            Boolean isNullableType = ReflectionCache.IsNullableType(enumType);

            if (isNullableType)
            {
                enumType = ReflectionCache.GetUnderlyingTypeForNullable(enumType);
            }

            foreach (var thing in System.Enum.GetValues(enumType))
            {
                var value = new EnumValue<T>();
                value.Id = (Int32)thing;
                value.Value = (T)thing;
                value.Name = System.Enum.GetName(enumType, thing);
                yield return value;
            }
        }

        public class EnumValue<T>
        {
            public Int32 Id { get; set; }
            public T Value { get; set; }
            public String Name { get; set; }
        }
    }
}
