using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Destrier
{
    //TODO: Refactor some of these things to make them less confusing.
    //TODO: Enforce CanBeNull etc.
    //TODO: Break PrimaryKey, AutoIdentity etc. out into their own attributes.
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ColumnAttribute : System.Attribute, IPopulate
    {
        /// <summary>
        /// The column name if different than the property name. Defaults to the property name.
        /// </summary>
        /// <remarks>
        /// Defaults to property name.
        /// </remarks>
        public String Name { get; set; }

        /// <summary>
        /// The database type if different than the natural CLR->SQL type mapping of the property type.
        /// </summary>
        /// <remarks>
        /// Defaults to whatever type the property is.
        /// </remarks>
        public System.Data.DbType? SqlDbType { get; set; }

        /// <summary>
        /// Denotes if the column is, or is part of, the primary key for the table.
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public Boolean IsPrimaryKey { get; set; }

        /// <summary>
        /// Denotes if the column is an IDENTITY(x,y) column
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public Boolean IsAutoIdentity { get; set; }

        /// <summary>
        /// Determines if the column needs to be a value or if it will be written / read as DBNull.Value.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public Boolean CanBeNull { get; set; }

        /// <summary>
        /// Do not try and write this column down for updates / inserts; it is only used when reading out result sets.
        /// </summary>
        public Boolean IsForReadOnly { get; set; }

        /// <summary>
        /// Determines the maximum length of property if it is a string. Ignored for other types.
        /// </summary>
        /// <remarks>
        /// Defaults to whatever length the string is.
        /// </remarks>
        public Int32 MaxStringLength { get; set; }

        /// <summary>
        /// If true, will substring the value up to the maximum length.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// Will not affect anything if there is no MaxStringLength specified.
        /// </remarks>
        public Boolean ShouldTrimLongStrings { get; set; }

        public ColumnAttribute()
            : base()
        {
            IsPrimaryKey = false;
            IsAutoIdentity = false;
            IsForReadOnly = false;
            CanBeNull = true;
            ShouldTrimLongStrings = true;
        }

        public void Populate(IndexedSqlDataReader dr)
        {
            this.Name = dr.Get<String>("name");
            this.CanBeNull = dr.Get<Boolean>("is_nullable");
            this.MaxStringLength = dr.Get<Int32>("max_length");
            this.IsAutoIdentity = dr.Get<Boolean>("is_identity");
            this.IsPrimaryKey = dr.Get<Boolean>("is_primarykey");
        }
    }
}
