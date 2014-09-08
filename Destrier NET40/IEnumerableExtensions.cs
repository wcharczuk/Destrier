using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Destrier
{
    public static class IEnumerableExtensions
    {
        public static String ToCSV<T>(this IEnumerable<T> data)
        {
            if (data == null || !data.Any())
                return String.Empty;

            return String.Join(",", data.Where(_ => _ != null).Select(d => d.ToString()));
        }
        public static void DumpToStream<T>(this IEnumerable<T> objects, Stream outputStream)
        {
            var type = typeof(T);
            if (type.IsValueType || type.Equals(typeof(String)))
            {
                using (var sw = new StreamWriter(outputStream))
                {
                    sw.WriteLine("Value");
                    foreach (var obj in objects)
                    {
                        sw.WriteLine(obj.ToString());
                    }
                }
            }
            else
            {
                var properties = ReflectionHelper.GetProperties(type).Select(p => p.Value.Name).OrderBy(_ => _);
                using (var sw = new StreamWriter(outputStream))
                {
                    sw.WriteLine(properties.ToCSV());
                    foreach (var obj in objects)
                    {
                        var values = properties.Select(p => ReflectionHelper.GetValueOfPropertyForObject(obj, p)).Select(_ => _ != null ? _.ToString() : "-");
                        sw.WriteLine(values.ToCSV());
                    }
                }
            }
        }
    }
}
