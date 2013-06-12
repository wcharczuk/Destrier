using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Destrier
{
    public class TablePrimitive
    {
        public TablePrimitive(PropertyInfo referencedObjectProperty)
        {
            this.Property = referencedObjectProperty;
        }

        public PropertyInfo Property { get; set; }

        public String Database { get; set; }
        public String Schema { get; set; }
        public String TableName { get; set; }
        public String Alias { get; set; }

        public string FullyQualifiedTableName
        {
            get
            {
                return String.Format("{0}.{1}.{2}", this.Database, this.Schema, this.TableName);
            }
        }
    }
}
