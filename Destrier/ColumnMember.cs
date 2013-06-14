using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Destrier
{
    /// <summary>
    /// This class is used when enumerating output columns.
    /// </summary>
    public class ColumnMember : Member
    {
        public ColumnMember(PropertyInfo pi)
            : base(pi)
        {
            this.ColumnAttribute = ReflectionCache.GetColumnAttribute(pi);
            if (ColumnAttribute != null)
            {
                //these are commonly accessed properties
                this.IsPrimaryKey = ColumnAttribute.IsPrimaryKey;
                this.IsAutoIdentity = ColumnAttribute.IsAutoIdentity;
                this.IsForReadOnly = ColumnAttribute.IsForReadOnly;

                this.Name = ColumnAttribute.Name ?? pi.Name;
                this.Skip = ColumnAttribute.IsForReadOnly;
            }
        }

        public ColumnAttribute ColumnAttribute { get; set; }

        public Boolean IsPrimaryKey { get; set; }
        public Boolean IsAutoIdentity { get; set; }
        public Boolean IsForReadOnly { get; set; }

        public Boolean Skip { get; set; }

        public String OutputAlias { get; set; }
    }
}