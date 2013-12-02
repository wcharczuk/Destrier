using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

namespace Destrier
{
    public class IndexedSqlDataReader : IDataReader, IDisposable
    {
        public IndexedSqlDataReader() : base() 
        {
            ResultSetIndex = 0;
        }

        public IndexedSqlDataReader(IDataReader hostReader, Boolean standardizeCasing = true) : this()
        {
            _dr = hostReader;
            StandardizeCasing = standardizeCasing;
            InitResultSet();
        }

        public IndexedSqlDataReader(IDataReader hostReader, Type type, Boolean standardizeCasing = false)
            : this()
        {
            _dr = hostReader;
            StandardizeCasing = standardizeCasing;
            CurrentOutputType = type;
            InitResultSet();
        }

        private void InitResultSet()
        {
            //these are standard regardless of if we're using this reader to get a destrier object.
            ColumnMap = _dr.GetColumnMap(this.StandardizeCasing);
            ColumnIndexMap = _dr.GetColumnIndexMap(this.StandardizeCasing);

            if (this.CurrentOutputType != null)
            {
                this.HasChildCollectionMembers = ModelCache.HasChildCollectionMembers(this.CurrentOutputType);
                this.HasReferencedObjectMembers = ModelCache.HasReferencedObjectMembers(this.CurrentOutputType);

                ColumnMemberLookup = this.StandardizeCasing ? ModelCache.GetColumnMemberStandardizedLookup(CurrentOutputType) : ModelCache.GetColumnMemberLookup(CurrentOutputType);

                var cm_index = new List<ColumnMember>();
                //column member index map gen
                foreach (var name in ColumnIndexMap)
                {
                    ColumnMember member = null;
                    if (ColumnMemberLookup.TryGetValue(name, out member))
                    {
                        cm_index.Add(member);
                    }
                }
                ColumnMemberIndexMap = cm_index.ToArray();

                if (!HasReferencedObjectMembers)
                {
                    _setInstanceValuesFn = ModelCache.GetSetInstanceValuesDelegate(this);
                }
            }
        }

        private ModelCache.SetInstanceValuesDelegate _setInstanceValuesFn = null;
		private IDataReader _dr = null;
		private Int32 _resultSetIndex = 0;

        /// <summary>
        /// Whether or not to standardize (ToLower) the casing of column names and member names.
        /// </summary>
        public Boolean StandardizeCasing { get; set; }

        /// <summary>
        /// The type to map to the current result set. Is optional.
        /// </summary>
		public Type CurrentOutputType { get; set; }

        /// <summary>
        /// The current index of the result set.
        /// </summary>
        public Int32 ResultSetIndex { get { return _resultSetIndex; } private set { _resultSetIndex = value; } }

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

        /// <summary>
        /// Returns true if the result set contains the columnName
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public Boolean HasColumn(String columnName)
        {
            if (this.StandardizeCasing)
                return ColumnMap.ContainsKey(Model.StandardizeCasing(columnName));
            else
                return ColumnMap.ContainsKey(columnName);
        }

        /// <summary>
        /// Returns the column index of the column name. Will standardize the name, does a dictionary lookup and will return null if not present.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Gets the name of the column at the index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public String GetColumnName(Int32 index)
        {
            return ColumnIndexMap[index];
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
            ResultSetIndex++;
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
        #endregion

        #region Get

        public T Get<T>(String columnName) 
        {
            var columnIndex = GetColumnIndex(columnName);
            if (columnIndex != null)
                return (T)Get(typeof(T), columnIndex.Value);
            else
                return (T)ReflectionHelper.GetDefault(typeof(T));
            
        }

        public T Get<T>(Int32 columnIndex)
        {
            return (T)Get(typeof(T), columnIndex);
        }

        public object Get(ColumnMember member, String columnName = null)
        {
            var columnIndex = GetColumnIndex(columnName ?? member.Name);
            if (columnIndex != null)
            {
                return Get(member.Type, columnIndex.Value, member.IsNullableType, member.UnderlyingGenericType);
            }
            return ReflectionHelper.GetDefault(member.Type);
        }

        public object Get(Type type, Int32 columnIndex, Boolean? isNullableType = null, Type underlyingType = null)
        {
            isNullableType = isNullableType ?? ReflectionHelper.IsNullableType(type);
            if (ReflectionHelper.IsNullableType(type))
            {
                if (!_dr.IsDBNull(columnIndex))
                {
                    underlyingType = underlyingType ?? ReflectionHelper.GetUnderlyingTypeForNullable(type);

                    var value = this.GetValue(columnIndex);

                    if (underlyingType.IsEnum)
                        return Enum.ToObject(underlyingType, value);
                    else
                        return Convert.ChangeType(value, underlyingType);
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
                return ReflectionHelper.GetDefault(type);
            }
        }

        public dynamic GetDynamic()
        {
            var value = new ExpandoObject();
            for (int i = 0; i < this.FieldCount; i++)
            {
                var name = this.GetName(i);
                ((IDictionary<string, object>)value)[name] = this.GetValue(i);
            }
            return value;
        }

        #endregion

        #region List Processing

        public dynamic ReadDynamic()
        {
            if(this.Read())
            {
                return GetDynamic();
            }
            return new ExpandoObject();
        }

        public List<dynamic> ReadDynamicList(Boolean advanceToNextResultAfter = false)
        {
            List<dynamic> values = new List<dynamic>();
            while (this.Read())
            {
                dynamic value = new ExpandoObject();
                for (int i = 0; i < this.FieldCount; i++)
                {
                    var name = this.GetName(i);
                    ((IDictionary<string, object>)value)[name] = this.GetValue(i);
                }
                values.Add(value);
            }
            
            return values;
        }

        public T ReadScalar<T>(Boolean advanceToNextResultAfter = true) where T : struct
        {
            T value = default(T);

            while (this.Read())
            {
                if (!this.IsDBNull(0))
                    value = (T)Convert.ChangeType(this.GetValue(0), typeof(T));
            }

            if (advanceToNextResultAfter)
                this.NextResult();

            return value;
        }

        public T ReadObject<T>(Boolean returnNullOnEmpty = false, Boolean advanceToNextResultAfter = true) where T : new()
        {
            T newObject = ReflectionHelper.GetNewObject<T>();
            bool hasPopulate = ReflectionHelper.HasInterface(typeof(T), typeof(IPopulate));

            while (this.Read())
            {
				if(hasPopulate)
					((IPopulate)newObject).Populate(this);
				else
					Model.Populate(newObject, this);
            }

            if (advanceToNextResultAfter)
                this.NextResult();

            return newObject;
        }

        public List<T> ReadScalarList<T>(Boolean advanceToNextResultAfter = true)
        {
            List<T> list = new List<T>();

            while (this.Read())
            {
                list.Add((T)Convert.ChangeType(this.GetValue(0), typeof(T)));
            }
            
            if (advanceToNextResultAfter)
                this.NextResult();

            return list;
        }

        public List<T> ReadList<T>(Boolean columnsCanBeMissing = false, Boolean advanceToNextResultAfter = true) where T : new()
        {
            List<T> list = new List<T>();

            bool hasPopulate = ReflectionHelper.HasInterface(typeof(T), typeof(IPopulate));
            while (this.Read())
            {
                T newObject = ReflectionHelper.GetNewObject<T>();
				if(hasPopulate)
					((IPopulate)newObject).Populate(this);
				else
					Model.Populate(newObject, this);

                list.Add(newObject);
            }

            if (advanceToNextResultAfter)
                this.NextResult();

            return list;
        }

        public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(Func<TValue, TKey> keySelector, Boolean advanceToNextResultAfter = true) where TValue : new()
        {
            Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
            bool hasPopulate = ReflectionHelper.HasInterface(typeof(TValue), typeof(IPopulate));

            while (this.Read())
            {
                TValue newObject = ReflectionHelper.GetNewObject<TValue>();

				if(hasPopulate)
					((IPopulate)newObject).Populate(this);
				else
					Model.Populate(newObject, this);

                TKey keyValue = keySelector(newObject);

                if (!dict.ContainsKey(keyValue))
                    dict.Add(keyValue, newObject);
            }
            

            if (advanceToNextResultAfter)
                this.NextResult();

            return dict;
        }

        public void ReadIntoParentCollection(Type type, Action<IndexedSqlDataReader, object> doStuffToAddToParent, Boolean advanceToNextResultAfter = true, Boolean populateFullResults = false)
		{
            bool hasPopulate = populateFullResults ? false : ReflectionHelper.HasInterface(type, typeof(IPopulate));
            while (this.Read())
            {
                var newObject = ReflectionHelper.GetNewObject(type);

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
            

            if (advanceToNextResultAfter)
                this.NextResult();
        }

        public void ReadIntoParentCollection<T>(Action<IndexedSqlDataReader, T> doStuffToAddToParent, Boolean advanceToNextResultAfter = true) where T : new()
        {
            bool hasPopulate = ReflectionHelper.HasInterface(typeof(T), typeof(IPopulate));
            while (this.Read())
            {
                T newObject = ReflectionHelper.GetNewObject<T>();
				if(hasPopulate)
					((IPopulate)newObject).Populate(this);
				else
					Model.Populate(newObject, this);

                doStuffToAddToParent(this, newObject);
            }
            

            if (advanceToNextResultAfter)
                this.NextResult();
        }

        public void ReadIntoParentCollectionDynamic(Action<IndexedSqlDataReader, dynamic> doStuffToAddToParent, Boolean advanceToNextResultAfter = true)
        {
            while (this.Read())
            {
                doStuffToAddToParent(this, this.GetDynamic());
            }

            if (advanceToNextResultAfter)
                this.NextResult();
        }

        public void ReadFullControl(Action<IndexedSqlDataReader> action, Boolean advanceToNextResultAfter = true)
        {
            while (this.Read())
            {
                action(this);
            }

            if (advanceToNextResultAfter)
                this.NextResult();
        }
        #endregion

        #region Util

        /// <summary>
        /// Uses the generated dynamic method to populate the instance of the <c>CurrentOutputType</c>
        /// </summary>
        /// <param name="instance"></param>
        /// <remarks>Will only set properties on the instance itself, not on any referenced objects etc.</remarks>
        public void SetInstanceValues(object instance)
        {
            _setInstanceValuesFn(this, instance);
        }

        /// <summary>
        /// Throws a data exception produced by the _setInstanceValuesFn.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="columnIndex"></param>
        /// <param name="instance"></param>
        /// <param name="reader"></param>
        public static void ThrowDataException(Exception ex, Int32 columnIndex, Object instance, IndexedSqlDataReader reader)
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

        public override bool Equals(object obj)
        {
            var them = obj as IndexedSqlDataReader;
            if (obj == null)
            {
                return false;
            }

            if(this.ColumnIndexMap.Length != them.ColumnIndexMap.Length)
                return false;

            Boolean isEqual = true;
            for(int x = 0; x < this.ColumnIndexMap.Length; x++)
            {
                var thisColumn = this.ColumnIndexMap[x];
                var themColumn = them.ColumnIndexMap[x];
                isEqual = isEqual && thisColumn.Equals(themColumn);
            }
            isEqual = isEqual && them.CurrentOutputType.Equals(this.CurrentOutputType);
            isEqual = isEqual && them.ResultSetIndex.Equals(this.ResultSetIndex);

            return isEqual;
        }

        public string GetCacheId()
        {
            var bigString = String.Join("|", this.ColumnIndexMap);
            if (this.CurrentOutputType != null)
            {
                return String.Format("{0}__{1}__{2}", this.CurrentOutputType.ToString(), this.ResultSetIndex, bigString);
            }
            else
                return String.Format("{0}__{1}", this.ResultSetIndex, bigString);
        }

        public override int GetHashCode()
        {
            throw new InvalidOperationException("Don't try and cache these in a dictionary!");            
        }

        public static implicit operator IndexedSqlDataReader(SqlDataReader dr)
        {
            return new IndexedSqlDataReader(dr);
        }

        public static implicit operator IndexedSqlDataReader(DbDataReader dr)
        {
            return new IndexedSqlDataReader(dr);
        }

        void IDisposable.Dispose()
        {
            if (_dr != null)
            {
                _dr.Dispose();
                _dr = null;
            }
        }
        #endregion
    }
}
