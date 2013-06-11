using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ReferencedObjectAttribute : System.Attribute
    {
        public ReferencedObjectAttribute() { PropertyName = null; }

        public ReferencedObjectAttribute(String propertyName)
        {
            this.PropertyName = propertyName;
        }

        /// <summary>
        /// This is the column that is the primary key on which to join for the extra columns.
        /// </summary>
        public String PropertyName { get; set; }
    }
}
