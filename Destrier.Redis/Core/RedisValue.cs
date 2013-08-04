using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier.Redis.Core
{
    public struct RedisValue
    {
        public bool IsNull;
        public long? LongValue;
        public string StringValue;

        public Boolean IsSuccess
        {
            get
            {
                if (!String.IsNullOrEmpty(this.StringValue))
                    return !StringValue.StartsWith("-");

                if (LongValue != null)
                    return LongValue.Value.Equals(1);
                
                return true;
            }
        }

        public DateTime? DateTimeValue
        {
            get
            {
                if (LongValue != null)
                    return RedisDataFormatUtil.FromUnixTimeStamp(LongValue.Value);
                else
                    return null;
            }
            set
            {
                if (value != null)
                    LongValue = RedisDataFormatUtil.ToUnixTimestamp(value.Value);
                else
                    LongValue = null;
            }
        }

        public override string ToString()
        {
            if (IsNull)
                return null;

            if (!IsSuccess)
                return "Error: " + StringValue;

            if (LongValue != null)
                return LongValue.Value.ToString();
            else if (!String.IsNullOrEmpty(StringValue))
                return StringValue;
            else
                return String.Empty;
        }
    }
}
