using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier.Redis.Core
{
    public struct RedisDataValue
    {
        public bool IsNull;
        public long LongValue;
        public string StringValue;
        public byte[] BinaryValue;

        public Boolean IsSuccess
        {
            get
            {
                if (!String.IsNullOrEmpty(this.StringValue))
                    return !StringValue.StartsWith("-");

                return true;
            }
        }

        public DateTime DateTimeValue
        {
            get
            {
                return RedisDataFormat.FromUnixTimeStamp(LongValue);
            }
            set
            {
                LongValue = RedisDataFormat.ToUnixTimestamp(value);
            }
        }
    }
}
