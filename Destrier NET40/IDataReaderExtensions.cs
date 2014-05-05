using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;

namespace Destrier
{
    public static class IDataReaderExtensions
    {
        public static Dictionary<String, Int32> GetColumnNameMap(this IDataReader dr)
        {
            var hash = new Dictionary<String, Int32>();
            for (int i = 0; i < dr.FieldCount; i++)
            {
                var name = Model.StandardizeCasing(dr.GetName(i));
                if (!hash.ContainsKey(name))
                    hash.Add(name, i);
            }
            return hash;
        }

        public static string[] GetColumnIndexMap(this IDataReader dr)
        {
            String[] strings = new string[dr.FieldCount];
            for (int i = 0; i < dr.FieldCount; i++)
            {
                strings[i] = Model.StandardizeCasing(dr.GetName(i));
            }
            return strings;
        }
    }
}