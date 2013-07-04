using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

namespace Destrier
{
    public class IndexedSqlDataReader : IDataReader
    {
        public IndexedSqlDataReader() : base() { }

        public IndexedSqlDataReader(SqlDataReader hostReader, Boolean standardizeCasing = true)
        {
            _dr = hostReader;
            StandardizeCasing = standardizeCasing;
            InitResultSet();
        }

        public IndexedSqlDataReader(SqlDataReader hostReader, Type type, Boolean standardizeCasing = false)
        {
            _dr = hostReader;
            StandardizeCasing = standardizeCasing;
            CurrentOutputType = type;
            InitResultSet();
        }

        public Boolean StandardizeCasing { get; set; }

        private SqlDataReader _dr = null;

        public Type CurrentOutputType { get; set; }

        /// <summary>
        /// Whether or not the output type needs to have child collections populated.
        /// </summary>
        public Boolean HasChildCollectionMembers { get; set; }

        /// <summary>
        /// Whether or not the output type needs to have referenced objects populated.
        /// </summary>
        public Boolean HasReferencedObjectMembers { get; set; }

        /// <summary>
        /// If the output type is specified this is a mapping between column names and column members.
        /// </summary>
        public Dictionary<String, ColumnMember> ColumnMemberLookup { get; set; }

        /// <summary>
        /// If the output type is specified this is a mapping between array indices and column members.
        /// </summary>
        public ColumnMember[] ColumnMemberIndexMap { get; set; }

        /// <summary>
        /// This is the mapping of column names to array indices.
        /// </summary>
        public Dictionary<String, Int32> ColumnMap { get; set; }

        /// <summary>
        /// This is the mapping of array indices to column names.
        /// </summary>
        public string[] ColumnIndexMap { get; set; }

        private ReflectionCache.SetInstanceValuesDelegate _setInstanceValuesFn = null;

        private void InitResultSet()
        {
            //these are standard regardless of if we're using this reader to get a destrier object.
            ColumnMap = _dr.GetColumnMap(this.StandardizeCasing);
            ColumnIndexMap = _dr.GetColumnIndexMap(this.StandardizeCasing);

            if (this.CurrentOutputType != null)
            {
                this.HasChildCollectionMembers = ReflectionCache.HasChildCollectionMembers(this.CurrentOutputType);
                this.HasReferencedObjectMembers = ReflectionCache.HasReferencedObjectMembers(this.CurrentOutputType);

                ColumnMemberLookup = ReflectionCache.GetColumnMemberLookup(CurrentOutputType);

                var cm_index = new List<ColumnMember>();
                //column member index map gen
                foreach (var name in ColumnIndexMap)
                {
                    ColumnMember member = null;
                    if(ColumnMemberLookup.TryGetValue(name, out member))
                    {
                        cm_index.Add(member);
                    }
                }
                ColumnMemberIndexMap = cm_index.ToArray();
                _setInstanceValuesFn = ReflectionCache.GenerateSetInstanceValuesDelegate(this);
            }
        }

        public Boolean HasColumn(String columnName)
        {
            if (this.StandardizeCasing)
                return ColumnMap.ContainsKey(Model.StandardizeCasing(columnName));
            else
                return ColumnMap.ContainsKey(columnName);
        }

        public Int32? GetColumnIndex(String columnName)
        {
            if (this.StandardizeCasing)
            {
                var columnNameLower = Model.StandardizeCasing(columnName);
                if (ColumnMap.ContainsKey(columnNameLower))
                    return ColumnMap[columnNameLower];
                else
                    return null;
            }
            else
            {
                if (ColumnMap.ContainsKey(columnName))
                    return ColumnMap[columnName];
                else
                    return null;
            }

        }

        public String GetColumnName(Int32 index)
        {
            return ColumnIndexMap[index];
        }

        public Boolean HasRows
        {
            get
            {
                return _dr.HasRows;
            }
        }

        #region IDataReader
        public void Close()
        {
            _dr.Close();
        }

        public int Depth
        {
            get { return _dr.Depth; }
        }

        public DataTable GetSchemaTable()
        {
            return _dr.GetSchemaTable();
        }

        public bool IsClosed
        {
            get { return _dr.IsClosed; }
        }

        public bool NextResult()
        {
            var result = _dr.NextResult();
            InitResultSet();
            return result;
        }

        public bool NextResult(Type type)
        {
            CurrentOutputType = type;
            return NextResult();
        }

        public bool Read()
        {
            return _dr.Read();
        }

        public int RecordsAffected
        {
            get { return _dr.RecordsAffected; }
        }

        public void Dispose()
        {
            _dr.Dispose();
        }

        public int FieldCount
        {
            get { return ColumnIndexMap.Length; }
        }

        public bool GetBoolean(int i)
        {
            return _dr.GetBoolean(i);
        }

        public byte GetByte(int i)
        {
            return _dr.GetByte(i);
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            return _dr.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
        }

        public char GetChar(int i)
        {
            return _dr.GetChar(i);
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            return _dr.GetChars(i, fieldoffset, buffer, bufferoffset, length);
        }

        public IDataReader GetData(int i)
        {
            return _dr.GetData(i);
        }

        public string GetDataTypeName(int i)
        {
            return _dr.GetDataTypeName(i);
        }

        public DateTime GetDateTime(int i)
        {
            return _dr.GetDateTime(i);
        }

        public decimal GetDecimal(int i)
        {
            return _dr.GetDecimal(i);
        }

        public double GetDouble(int i)
        {
            return _dr.GetDouble(i);
        }

        public Type GetFieldType(int i)
        {
            return _dr.GetFieldType(i);
        }

        public float GetFloat(int i)
        {
            return _dr.GetFloat(i);
        }

        public Guid GetGuid(int i)
        {
            return _dr.GetGuid(i);
        }

        public short GetInt16(int i)
        {
            return _dr.GetInt16(i);
        }

        public int GetInt32(int i)
        {
            return _dr.GetInt32(i);
        }

        public long GetInt64(int i)
        {
            return _dr.GetInt64(i);
        }

        public string GetName(int i)
        {
            return _dr.GetName(i);
        }

        public int GetOrdinal(string name)
        {
            return _dr.GetOrdinal(name);
        }

        public string GetString(int i)
        {
            return _dr.GetString(i);
        }

        public object GetValue(int i)
        {
            return _dr.GetValue(i);
        }

        public object GetValue(int i, TypeCode outputType)
        {
            var originType = Type.GetTypeCode(_dr.GetFieldType(i));
            switch (outputType)
            {
                case TypeCode.Boolean:
                    return _dr.GetBoolean(i);
                case TypeCode.Byte:
                    return _dr.GetByte(i);
                case TypeCode.Char:
                case TypeCode.String:
                    switch (originType)
                    {
                        case TypeCode.DateTime:
                            return _dr.GetDateTime(i).ToString();
                        case TypeCode.Boolean:
                            return _dr.GetBoolean(i).ToString();
                        case TypeCode.Int16:
                            return _dr.GetInt16(i).ToString();
                        case TypeCode.Int32:
                            return _dr.GetInt32(i).ToString();
                        case TypeCode.Int64:
                            return _dr.GetInt64(i).ToString();
                        case TypeCode.Double:
                            return _dr.GetDouble(i).ToString();
                        case TypeCode.Decimal:
                            return _dr.GetDecimal(i).ToString();
                        default:
                            return _dr.GetString(i);
                    }
                case TypeCode.DateTime:
                    return _dr.GetDateTime(i);
                case TypeCode.Single:
                    switch (originType)
                    {
                        case TypeCode.Byte:
                            return (float)_dr.GetByte(i);
                        case TypeCode.Int64:
                            return (float)_dr.GetInt64(i); //wut r u doing.
                        case TypeCode.Int32:
                            return (float)_dr.GetInt32(i);
                        case TypeCode.Int16:
                            return (float)_dr.GetInt16(i);
                        case TypeCode.Double:
                            return (float)_dr.GetDouble(i);
                        case TypeCode.Decimal:
                            return (float)_dr.GetDecimal(i);
                        default:
                            return (float)_dr.GetDouble(i);
                    }
                case TypeCode.Double:
                    switch (originType)
                    {
                        case TypeCode.Byte:
                            return (double)_dr.GetByte(i);
                        case TypeCode.Int64:
                            return (double)_dr.GetInt64(i); //wut r u doing.
                        case TypeCode.Int32:
                            return (double)_dr.GetInt32(i);
                        case TypeCode.Int16:
                            return (double)_dr.GetInt16(i);
                        case TypeCode.Single:
                            return (double)_dr.GetDouble(i);
                        case TypeCode.Decimal:
                            return (double)_dr.GetDecimal(i);
                        default:
                            return _dr.GetDouble(i);
                    }
                case TypeCode.Decimal:
                    return _dr.GetDecimal(i);
                case TypeCode.UInt16:
                case TypeCode.Int16:
                    switch(originType)
                    {
                        case TypeCode.Byte:
                            return (short)_dr.GetByte(i);
                        case TypeCode.Int64:
                            return (short)_dr.GetInt64(i); //wut r u doing.
                        case TypeCode.Int32:
                            return (short)_dr.GetInt32(i);
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return (short)_dr.GetDouble(i);
                        case TypeCode.Decimal:
                            return (short)_dr.GetDecimal(i);
                        default:
                            return _dr.GetInt16(i);
                    }
                case TypeCode.UInt32:
                case TypeCode.Int32:
                    switch (originType)
                    {
                        case TypeCode.Byte:
                            return (int)_dr.GetByte(i);
                        case TypeCode.Int64:
                            return (int)_dr.GetInt64(i);
                        case TypeCode.Int16:
                            return (int)_dr.GetInt16(i);
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return (int)_dr.GetDouble(i);
                        case TypeCode.Decimal:
                            return (int)_dr.GetDecimal(i);
                        default:
                            return _dr.GetInt32(i);
                    }
                    
                case TypeCode.UInt64:
                case TypeCode.Int64:
                    switch(originType)
                    {
                        case TypeCode.Byte:
                            return (long)_dr.GetByte(i);
                        case TypeCode.Int16:
                            return (long)_dr.GetInt16(i);
                        case TypeCode.Int32:
                            return (long)_dr.GetInt32(i);
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return (long)_dr.GetDouble(i);
                        case TypeCode.Decimal:
                            return (long)_dr.GetDecimal(i);
                        default:
                            return _dr.GetInt64(i);
                    }
                case TypeCode.Object:
                    return _dr.GetValue(i);
            }
            throw new InvalidOperationException("Cannot retrieve specified type: " + outputType.ToString());
        }

        public int GetValues(object[] values)
        {
            return _dr.GetValues(values);
        }

        public bool IsDBNull(int i)
        {
            return _dr.IsDBNull(i);
        }

        public object this[string name]
        {
            get 
            {
                return _dr[name];
            }
        }

        public object this[int i]
        {
            get
            {
                return _dr[i];
            }
        }

        public static implicit operator IndexedSqlDataReader(SqlDataReader dr)
        {
            return new IndexedSqlDataReader(dr);
        }
        #endregion

        #region Get
        public T Get<T>(String columnName) 
        {
            ColumnMember member = null;
            if (ColumnMemberLookup != null)
            {
                ColumnMemberLookup.TryGetValue(columnName, out member);
                if (member != null)
                    return (T)this.Get(member);
            }
            
            var columnIndex = GetColumnIndex(columnName);
            if (columnIndex != null)
                return (T)Get(typeof(T), columnIndex.Value);
            else
                return (T)ReflectionCache.GetDefault(typeof(T));
            
        }

        public object Get(ColumnMember member, String columnName = null)
        {
            var columnIndex = GetColumnIndex(columnName ?? member.Name);
            if (columnIndex != null)
            {
                return Get(member, columnIndex.Value);
            }
            return ReflectionCache.GetDefault(member.Type);
        }

        public object Get(ColumnMember member, Int32 columnIndex)
        {
            if (member.IsNullableType)
            {
                if (!_dr.IsDBNull(columnIndex))
                {
                    var resultType = member.NullableUnderlyingType;
                    var convertTo = Type.GetTypeCode(resultType);
                    object value = this.GetValue(columnIndex, convertTo);

                    if (resultType.IsEnum)
                        return Enum.ToObject(resultType, value);
                   
                    return value;
                }
                return null;
            }
            else
            {
                if (!_dr.IsDBNull(columnIndex))
                {
                    var resultType = member.Type;
                    var convertTo = Type.GetTypeCode(resultType);
                    object value = this.GetValue(columnIndex, convertTo);

                    if (resultType.IsEnum)
                        return Enum.ToObject(resultType, value);
                    
                    return value;
                }
                return ReflectionCache.GetDefault(member.Type);
            }
        }

        public object Get(Type type, Int32 columnIndex)
        {
            if (ReflectionCache.IsNullableType(type))
            {
                if (!_dr.IsDBNull(columnIndex))
                {
                    var resultType = ReflectionCache.GetUnderlyingTypeForNullable(type);
                    var value = _dr.GetValue(columnIndex);
                    if (resultType.IsEnum)
                        return Enum.ToObject(resultType, value);
                    else
                        return Convert.ChangeType(value, resultType);
                }
                return null;
            }
            else
            {
                if (!_dr.IsDBNull(columnIndex))
                {
                    object value = this.GetValue(columnIndex);
                    if (type.IsEnum)
                        return Enum.ToObject(type, value);
                    else
                        return Convert.ChangeType(value, type);
                }
                return ReflectionCache.GetDefault(type);
            }
        }

        #endregion

        public void SetInstanceValues(object instance)
        {
            _setInstanceValuesFn(this, instance);
        }

        public static void ThrowDataException(Exception ex, Int32 columnIndex, IndexedSqlDataReader reader)
        {
            Exception toThrow;
            try
            {
                string name = "-";
                string value = "-";
                string memberName = "-";
                string memberType = "-";
                if (reader != null && columnIndex >= 0 && columnIndex < reader.FieldCount)
                {
                    name = reader.GetName(columnIndex);
                    object val = reader.GetValue(columnIndex);
                    if (val == null || val is DBNull)
                    {
                        value = "null";
                    }
                    else
                    {
                        value = Convert.ToString(val) + " as " + Type.GetTypeCode(val.GetType());
                    }

                    if (reader.ColumnMemberIndexMap != null && reader.ColumnMemberIndexMap.Any())
                    {
                        memberName = reader.ColumnMemberIndexMap[columnIndex].Name;
                        memberType = reader.ColumnMemberIndexMap[columnIndex].Type.ToString();
                    }
                }
                toThrow = new DataException(String.Format("Exception reading column {0}: ({1} = {2}) for {3} as {4})", columnIndex, name, value, memberName, memberType), ex);
            }
            catch
            {
                toThrow = new DataException(ex.Message, ex);
            }
            throw toThrow;
        }

        #region List Processing
        public dynamic ReadDynamic()
        {
            var value = new AgileObject();
            if (this.HasRows)
            {
                while (this.Read())
                {
                    for (int i = 0; i < this.FieldCount; i++)
                    {
                        var name = this.GetName(i);
                        ((IDictionary<string, object>)value)[name] = this.GetValue(i);
                    }
                }
            }
            return value;
        }

        public List<AgileObject> ReadDynamicList(Boolean advanceToNextResultAfter = false)
        {
            List<AgileObject> values = new List<AgileObject>();
            if (this.HasRows)
            {
                while (this.Read())
                {
                    dynamic value = new AgileObject();
                    for (int i = 0; i < this.FieldCount; i++)
                    {
                        var name = this.GetName(i);
                        ((IDictionary<string, object>)value)[name] = this.GetValue(i);
                    }
                    values.Add(value);
                }
            }
            return values;
        }

        public T ReadScalar<T>(Boolean advanceToNextResultAfter = true) where T : struct
        {
            T value = default(T);

            if (this.HasRows)
            {
                while (this.Read())
                {
                    if (!this.IsDBNull(0))
                        value = (T)Convert.ChangeType(this.GetValue(0), typeof(T));
                }
            }

            if (advanceToNextResultAfter)
                this.NextResult();

            return value;
        }

        public T ReadObject<T>(Boolean returnNullOnEmpty = false, Boolean advanceToNextResultAfter = true) where T : new()
        {
            T newObject = ReflectionCache.GetNewObject<T>();
			bool hasPopulate = ReflectionCache.HasInterface(typeof(T), typeof(IPopulate));

            if (this.HasRows)
            {
                while (this.Read())
                {
					if(hasPopulate)
						((IPopulate)newObject).Populate(this);
					else
						Model.Populate(newObject, this);
                }
            }
            else if (returnNullOnEmpty)
            {
                newObject = default(T);
            }

            if (advanceToNextResultAfter)
                this.NextResult();

            return newObject;
        }

        public List<T> ReadScalarList<T>(Boolean advanceToNextResultAfter = true)
        {
            List<T> list = new List<T>();

            if (this.HasRows)
            {
                while (this.Read())
                {
                    list.Add((T)Convert.ChangeType(this.GetValue(0), typeof(T)));
                }
            }

            if (advanceToNextResultAfter)
                this.NextResult();

            return list;
        }

        public List<T> ReadList<T>(Boolean columnsCanBeMissing = false, Boolean advanceToNextResultAfter = true) where T : new()
        {
            List<T> list = new List<T>();

			bool hasPopulate = ReflectionCache.HasInterface(typeof(T), typeof(IPopulate));
            if (this.HasRows)
            {
                while (this.Read())
                {
                    T newObject = ReflectionCache.GetNewObject<T>();
					if(hasPopulate)
						((IPopulate)newObject).Populate(this);
					else
						Model.Populate(newObject, this);

                    list.Add(newObject);
                }
            }

            if (advanceToNextResultAfter)
                this.NextResult();

            return list;
        }

        public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(Func<TValue, TKey> keySelector, Boolean advanceToNextResultAfter = true) where TValue : new()
        {
            Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
			bool hasPopulate = ReflectionCache.HasInterface(typeof(TValue), typeof(IPopulate));

            if (this.HasRows)
            {
                while (this.Read())
                {
                    TValue newObject = ReflectionCache.GetNewObject<TValue>();

					if(hasPopulate)
						((IPopulate)newObject).Populate(this);
					else
						Model.Populate(newObject, this);

                    TKey keyValue = keySelector(newObject);

                    if (!dict.ContainsKey(keyValue))
                        dict.Add(keyValue, newObject);
                }
            }

            if (advanceToNextResultAfter)
                this.NextResult();

            return dict;
        }

        public void ReadIntoParentCollection(Type type, Action<IndexedSqlDataReader, object> doStuffToAddToParent, Boolean advanceToNextResultAfter = true, Boolean populateFullResults = false)
		{
			bool hasPopulate = populateFullResults ? false : ReflectionCache.HasInterface(type, typeof(IPopulate));
            if (this.HasRows)
            {
                while (this.Read())
                {
                    var newObject = ReflectionCache.GetNewObject(type);

                    if (populateFullResults)
                    {
                        Model.PopulateFullResults(newObject, this, type);
                    }
                    else
					{
						if(hasPopulate)
							((IPopulate)newObject).Populate(this);
						else
							Model.Populate(newObject, this);
					}

                    doStuffToAddToParent(this, newObject);
                }
            }

            if (advanceToNextResultAfter)
                this.NextResult();
        }

        public void ReadIntoParentCollection<T>(Action<IndexedSqlDataReader, T> doStuffToAddToParent, Boolean advanceToNextResultAfter = true) where T : new()
        {
			bool hasPopulate = ReflectionCache.HasInterface(typeof(T), typeof(IPopulate));
            if (this.HasRows)
            {
                while (this.Read())
                {
                    T newObject = ReflectionCache.GetNewObject<T>();
					if(hasPopulate)
						((IPopulate)newObject).Populate(this);
					else
						Model.Populate(newObject, this);

                    doStuffToAddToParent(this, newObject);
                }
            }

            if (advanceToNextResultAfter)
                this.NextResult();
        }

        public void ReadFullControl(Action<IndexedSqlDataReader> action, Boolean advanceToNextResultAfter = true)
        {
            if (this.HasRows)
            {
                while (this.Read())
                {
                    action(this);
                }
            }

            if (advanceToNextResultAfter)
                this.NextResult();
        }
        #endregion
    }
}
