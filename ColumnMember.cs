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
            var ca = Model.ColumnAttribute(pi);
            if (ca != null)
            {
                this.IsPrimaryKey = ca.IsPrimaryKey;
                this.Name = ca.Name ?? pi.Name;
                this.Skip = ca.IsForReadOnly;
            }
        }

        public Boolean IsPrimaryKey { get; set; }

        public Boolean Skip { get; set; }

        public String OutputAlias { get; set; }
    }
}