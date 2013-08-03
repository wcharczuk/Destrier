using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier.Redis.Core
{
    public static class RedisDataFormat
    {
        public static Encoding Encoding
        {
            get 
            {
                return System.Text.Encoding.UTF8;
            }
        }

        public static Int64 ToUnixTimestamp(DateTime value)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0);
            return (long)(value - epoch).TotalSeconds;
        }

        public static DateTime FromUnixTimeStamp(Int64 unixTimeStamp)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0);
            return epoch.AddSeconds(unixTimeStamp);
        }

        public static Byte[] FromLong(Int64 value)
        {
            return BitConverter.GetBytes(value);
        }

        public static Byte[] FromString(String value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        public static Int64 FormatAsLong(Byte[] buffer)
        {
            return BitConverter.ToInt64(buffer, 0);
        }

        public static String FormatAsString(Byte[] buffer)
        {
            return Encoding.Default.GetString(buffer);
        }
    }
}
