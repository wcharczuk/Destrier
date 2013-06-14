using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Destrier
{
    /// <summary>
    /// Represents a referenced object property, also conceptually a join.
    /// </summary>
    public class ReferencedObjectMember : Member
    {
        public ReferencedObjectMember(PropertyInfo pi) : base(pi)
        {
            this.ReferencedObjectAttribute = ReflectionCache.GetReferencedObjectAttribute(pi);
            this.ReferencedColumnMember = Model.ColumnMemberForPropertyName(DeclaringType, ReferencedObjectAttribute.PropertyName);
            this.ReferencedColumnIsNullable = ReflectionCache.IsNullableType(ReferencedColumnProperty.PropertyType) || ReferencedColumnAttribute.CanBeNull;

            this.TableName = Model.TableName(this.Type); 
            this.DatabaseName = Model.DatabaseName(this.Type); 
            this.SchemaName = Model.SchemaName(this.Type);
        }
        public ReferencedObjectAttribute ReferencedObjectAttribute { get; set; }

        public ColumnMember ReferencedColumnMember { get; set; }

        public PropertyInfo ReferencedColumnProperty { get { return ReferencedColumnMember.Property; } }
        public ColumnAttribute ReferencedColumnAttribute { get { return ReferencedColumnMember.ColumnAttribute; } }

        public Boolean ReferencedColumnIsNullable { get; set; }

        public String TableName { get; private set; }
        public String DatabaseName { get; private set; }
        public String SchemaName { get; set; }

        public String JoinType { get { return ReferencedColumnIsNullable ? "LEFT" : "INNER"; } }
        public String FullyQualifiedTableName { get { return String.Format("{0}.{1}.{2}", this.DatabaseName, this.SchemaName, this.TableName); } }

        public String AliasedParentColumnName 
        { 
            get 
            {
                var parentAlias = String.Empty;
                if (this.Parent != null)
                {
                    parentAlias = this.Parent.TableAlias;
                }
                else if (this.Root != null)
                {
                    parentAlias = this.Root.TableAlias;
                }
                return String.Format("[{0}].[{1}]", parentAlias, ReferencedColumnMember.Name);
            }
        }

        public String AliasedColumnName 
        { 
            get 
            {
                var pks = Model.ColumnsPrimaryKey(this.Type);
                if (pks.Count() > 1)
                {
                    throw new Exception("Multiple Primary Key, cannot join on this type yet.");
                }
                var pk = pks.FirstOrDefault();
                if (pk == null)
                {
                    throw new Exception("No Primary Key to join on.");
                }
                return String.Format("[{0}].[{1}]", this.TableAlias, pk.Name);
            }
        }
    }
}
