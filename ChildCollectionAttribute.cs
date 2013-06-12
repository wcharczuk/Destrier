using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ChildCollectionAttribute : System.Attribute
    {
        public ChildCollectionAttribute() { }
        public ChildCollectionAttribute(String columnName)
        {
            this.ColumnName = columnName;
            this.AlwaysInclude = false;
        }

        public ChildCollectionAttribute(String columnName, Boolean alwaysInclude)
        {
            this.ColumnName = columnName;
            this.AlwaysInclude = alwaysInclude;
        }

        /// <summary>
        /// This is the column name, in the child object, to join on to associate it with it's parent.
        /// </summary>
        public String ColumnName { get; set; }

        /// <summary>
        /// Always include the child collection in output. Default is 'false'.
        /// </summary>
        public Boolean AlwaysInclude { get; set; }
    }
}
