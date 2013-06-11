using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Destrier
{
    public class ChildCollectionMember : Member
    {
        public ChildCollectionMember() : base() { }

        public ChildCollectionMember(PropertyInfo pi)
            : base(pi)
        {
            var attribute = Model.ChildCollectionAttribute(pi);
            AlwaysInclude = attribute.AlwaysInclude;
            
            this.ParentPrimaryKeyProperty = Model.ColumnsPrimaryKey(pi.DeclaringType).FirstOrDefault();
            this.ParentPrimaryKeyColumnName = Model.ColumnName(ParentPrimaryKeyProperty);

            this.ReferencedColumnName = attribute.ColumnName ?? ParentPrimaryKeyColumnName;
            
            this.CollectionType = ReflectionCache.GetUnderlyingTypeForCollection(this.Type);
            this.ReferenceProperty = Model.ColumnPropertyForPropertyName(this.CollectionType, this.ReferencedColumnName);

            this.TableName = Model.TableName(this.CollectionType); 
            this.DatabaseName = Model.DatabaseName(this.CollectionType);
            this.SchemaName = Model.SchemaName(this.CollectionType);
        }

        public PropertyInfo ParentPrimaryKeyProperty { get; set; }
        public String ParentPrimaryKeyColumnName { get; set; }

        public Type CollectionType { get; set; }

        public Boolean AlwaysInclude { get; set; }

        /// <summary>
        /// This is the column on the child object we join to the parent.
        /// </summary>
        public String ReferencedColumnName { get; set; }
        public PropertyInfo ReferenceProperty { get; set; }

        public String TableName { get; private set; }
        public String DatabaseName { get; private set; }
        public String SchemaName { get; set; }

        public String FullyQualifiedTableName { get { return String.Format("{0}.{1}.{2}", this.DatabaseName, this.SchemaName, this.TableName); } }

        public String JoinType { get { return "INNER"; } }

        public override Boolean HasCycle
        {
            get
            {
                if (this.Parent == null)
                    return false;
                else
                    return base.ParentAny(p => p.Type.Equals(this.CollectionType));
            }
        }

        public String AliasedParentColumnName
        {
            get
            {
                var parentAlias = String.Empty;
                if (this.Parent != null)
                    parentAlias = this.Parent.TableAlias;
                else if (this.Root != null)
                    parentAlias = this.Root.TableAlias;
                
                return String.Format("[{0}].[{1}]", parentAlias, ParentPrimaryKeyColumnName);
            }
        }

        public String AliasedColumnName
        {
            get
            {
                if (String.IsNullOrEmpty(ReferencedColumnName))
                    throw new Exception("Bit of a whoopsie; no back referencing column name to work with.");

                return String.Format("[{0}].[{1}]", this.TableAlias, ReferencedColumnName);
            }
        }
    }
}
