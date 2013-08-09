using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier.Redis.Core
{
    public struct RedisValue : IConvertible
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

        private object Convert(TypeCode toType)
        {
            if (IsNull)
                return null;

            switch (toType)
            {
                case TypeCode.Boolean:
                    {
                        if (LongValue != null)
                            return LongValue.Value.Equals(1);
                        else if (StringValue != null)
                            return int.Parse(this.StringValue).Equals(1);
                        else
                            return null;
                    }
                case TypeCode.Byte:
                    {
                        if (LongValue != null)
                            return (Byte)LongValue.Value;
                        else if (StringValue != null)
                            return (Byte)int.Parse(this.StringValue);
                        else
                            return null;
                    }
                case TypeCode.SByte:
                    {
                        if (LongValue != null)
                            return (SByte)LongValue.Value;
                        else if (StringValue != null)
                            return (SByte)int.Parse(this.StringValue);
                        else
                            return null;
                    }
                case TypeCode.Char:
                    {
                        if (LongValue != null)
                            return (Char)LongValue.Value;
                        else if (!String.IsNullOrEmpty(this.StringValue))
                            return this.StringValue[0];
                        else
                            return null;
                    }
                case TypeCode.String:
                    {
                        if (StringValue != null)
                            return StringValue;
                        else if (LongValue != null)
                            return LongValue.Value.ToString();
                        else
                            return null;
                    }
                case TypeCode.Int16:
                    {
                        if (StringValue != null)
                            return Int16.Parse(StringValue);
                        else if (LongValue != null)
                            return (Int16)LongValue.Value;
                        else
                            return null;
                    }
                case TypeCode.Int32:
                    {
                        if (StringValue != null)
                            return Int32.Parse(StringValue);
                        else if (LongValue != null)
                            return (Int32)LongValue.Value;
                        else
                            return null;
                    }
                case TypeCode.Int64:
                    {
                        if (StringValue != null)
                            return Int64.Parse(StringValue);
                        else if (LongValue != null)
                            return (Int64)LongValue.Value;
                        else
                            return null;
                    }
                case TypeCode.UInt16:
                    {
                        if (StringValue != null)
                            return UInt16.Parse(StringValue);
                        else if (LongValue != null)
                            return (UInt16)LongValue.Value;
                        else
                            return null;
                    }
                case TypeCode.UInt32:
                    {
                        if (StringValue != null)
                            return UInt32.Parse(StringValue);
                        else if (LongValue != null)
                            return (UInt32)LongValue.Value;
                        else
                            return null;
                    }
                case TypeCode.UInt64:
                    {
                        if (StringValue != null)
                            return UInt64.Parse(StringValue);
                        else if (LongValue != null)
                            return (UInt64)LongValue.Value;
                        else
                            return null;
                    }
                case TypeCode.Single:
                    {
                        if (StringValue != null)
                            return Single.Parse(StringValue);
                        else if (LongValue != null)
                            return (Single)LongValue.Value;
                        else
                            return null;
                    }
                case TypeCode.Double:
                    {
                        if (StringValue != null)
                            return Double.Parse(StringValue);
                        else if (LongValue != null)
                            return (Double)LongValue.Value;
                        else
                            return null;
                    }
                case TypeCode.Decimal:
                    {
                        if (StringValue != null)
                            return Decimal.Parse(StringValue);
                        else if (LongValue != null)
                            return (Decimal)LongValue.Value;
                        else
                            return null;
                    }
                case TypeCode.DateTime:
                    {
                        if (StringValue != null)
                            return DateTime.Parse(StringValue);
                        else if (LongValue != null)
                            return RedisDataFormatUtil.FromUnixTimeStamp(LongValue.Value);
                        else
                            return null;
                    }
                default:
                case TypeCode.Object:
                    {
                        if (StringValue != null)
                            return StringValue;
                        else if (LongValue != null)
                            return LongValue.Value;
                        else
                            return null;
                    }
            }
        }

        public TypeCode GetTypeCode()
        {
            if (StringValue != null)
                return TypeCode.String;
            else if (LongValue != null)
                return TypeCode.Int64;
            else if (this.IsNull)
                return TypeCode.DBNull;
            else
                return TypeCode.Empty;
        }

        public bool ToBoolean(IFormatProvider provider = null)
        {
            var value = Convert(TypeCode.Boolean);
            return value != null ? (Boolean)value : default(Boolean);
        }

        public byte ToByte(IFormatProvider provider = null)
        {
            var value = Convert(TypeCode.Byte);
            return value != null ? (Byte)value : default(Byte);
        }

        public char ToChar(IFormatProvider provider = null)
        {
            var value = Convert(TypeCode.Char);
            return value != null ? (Char)value : default(Char);
        }

        public DateTime ToDateTime(IFormatProvider provider = null)
        {
            var value = Convert(TypeCode.DateTime);
            return value != null ? (DateTime)value : default(DateTime);
        }

        public decimal ToDecimal(IFormatProvider provider = null)
        {
            var value = Convert(TypeCode.Decimal);
            return value != null ? (Decimal)value : default(Decimal);
        }

        public double ToDouble(IFormatProvider provider = null)
        {
            var value = Convert(TypeCode.Double);
            return value != null ? (Double)value : default(Double);
        }

        public short ToInt16(IFormatProvider provider = null)
        {
            var value = Convert(TypeCode.Int16);
            return value != null ? (Int16)value : default(Int16);
        }

        public int ToInt32(IFormatProvider provider = null)
        {
            var value = Convert(TypeCode.Int32);
            return value != null ? (Int32)value : default(Int32);
        }

        public long ToInt64(IFormatProvider provider = null)
        {
            var value = Convert(TypeCode.Int64);
            return value != null ? (Int64)value : default(Int64);
        }

        public sbyte ToSByte(IFormatProvider provider = null)
        {
            var value = Convert(TypeCode.SByte);
            return value != null ? (SByte)value : default(SByte);
        }

        public float ToSingle(IFormatProvider provider = null)
        {
            var value = Convert(TypeCode.Single);
            return value != null ? (Single)value : default(Single);
        }

        public string ToString(IFormatProvider provider = null)
        {
            var value = Convert(TypeCode.String);
            return value != null ? (String)value : null;
        }

        public object ToType(Type conversionType, IFormatProvider provider = null)
        {
            if (this.StringValue != null)
                return typeof(String);
            else if (this.LongValue != null)
                return typeof(Int64);
            else if (this.IsNull)
                return typeof(DBNull);
            else
                return typeof(RedisValue);
        }

        public ushort ToUInt16(IFormatProvider provider = null)
        {
            var value = Convert(TypeCode.UInt16);
            return value != null ? (UInt16)value : default(UInt16);
        }

        public uint ToUInt32(IFormatProvider provider = null)
        {
            var value = Convert(TypeCode.UInt32);
            return value != null ? (UInt32)value : default(UInt32);
        }

        public ulong ToUInt64(IFormatProvider provider = null)
        {
            var value = Convert(TypeCode.UInt64);
            return value != null ? (UInt64)value : default(UInt64);
        }
    }
}
