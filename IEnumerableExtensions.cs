using System;
using System.Collections.Generic;
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

            return String.Join(",", data.Select(d => d.ToString()));
        }
    }
}
