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
        public ChildCollectionAttribute(String childColumnName)
        {
            this.ChildColumnName = childColumnName;
            this.AlwaysInclude = false;
        }

        public ChildCollectionAttribute(String childColumnName, String parentColumnName)
        {
            this.ChildColumnName = childColumnName;
            this.ParentColumnName = parentColumnName;
        }

        public ChildCollectionAttribute(String childColumnName, Boolean alwaysInclude)
        {
            this.ChildColumnName = childColumnName;
            this.AlwaysInclude = alwaysInclude;
        }

        public ChildCollectionAttribute(String childColumnName, String parentColumnName, Boolean alwaysInclude)
        {
            this.ChildColumnName = childColumnName;
            this.ParentColumnName = parentColumnName;
            this.AlwaysInclude = alwaysInclude;
        }

        /// <summary>
        /// This is the column name to join on in the child table.
        /// </summary>
        public String ChildColumnName { get; set; }

        /// <summary>
        /// This is the column name to join on in the parent table.
        /// </summary>
        /// <remarks>Optional. If unset, is assumed to be the parents primary key.</remarks>
        public String ParentColumnName { get; set; }

        /// <summary>
        /// Always include the child collection in output. Default is 'false'.
        /// </summary>
        public Boolean AlwaysInclude { get; set; }
    }
}
