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
            var attribute = ModelCache.GetChildCollectionAttribute(pi);
            AlwaysInclude = attribute.AlwaysInclude;

            if (this.IsLazy)
                this.CollectionType = ReflectionHelper.GetUnderlyingTypeForCollection(this.UnderlyingGenericType);
            else
                this.CollectionType = ReflectionHelper.GetUnderlyingTypeForCollection(this.Type);

            if (!String.IsNullOrWhiteSpace(attribute.ParentColumnName))
                this.ParentReferencedMember = Model.ColumnMemberForPropertyName(pi.DeclaringType, attribute.ParentColumnName);
            else
                this.ParentReferencedMember = Model.ColumnsPrimaryKey(pi.DeclaringType).FirstOrDefault();
            
            if(!String.IsNullOrWhiteSpace(attribute.ChildColumnName))
                this.ReferencedColumn = Model.ColumnMemberForPropertyName(this.CollectionType, attribute.ChildColumnName);
            else
                this.ReferencedColumn = Model.ColumnMemberForPropertyName(this.CollectionType, this.ParentReferencedColumnName);

            this.TableName = Model.TableName(this.CollectionType); 
            this.DatabaseName = Model.DatabaseName(this.CollectionType);
            this.SchemaName = Model.SchemaName(this.CollectionType);
            this.UseNoLock = Model.UseNoLock(this.CollectionType);
        }

        /// <summary>
        /// Used in join predicates, it denotes what column member to reference when joining a child collection back to its parent.
        /// </summary>
        public ColumnMember ParentReferencedMember { get; set; }

        /// <summary>
        /// This is the property, on the parent object, that has the primary key value for the parent reference from the child.
        /// </summary>
        public PropertyInfo ParentReferencedProperty { get { return ParentReferencedMember.Property; } }

        /// <summary>
        /// This is the column name of the parent reference, used when joining. 
        /// </summary>
        public String ParentReferencedColumnName { get { return ParentReferencedMember.Name; } }
        
        /// <summary>
        /// This is the T in List[T], a.k.a. what you have a list of.
        /// </summary>
        public Type CollectionType { get; set; }

        /// <summary>
        /// Whether or not to always add this to the enumerated child collection list. 
        /// </summary>
        public Boolean AlwaysInclude { get; set; }

        /// <summary>
        /// What column member on the child model object to join against.
        /// </summary>
        public ColumnMember ReferencedColumn { get; set; }

        /// <summary>
        /// The property that has the referenced column defined on it in the model.
        /// </summary>
        public PropertyInfo ReferencedProperty { get { return ReferencedColumn.Property; } }

        /// <summary>
        /// The name of the child schema column to join to the parent.
        /// </summary>
        public String ReferencedColumnName { get { return ReferencedColumn.Name; } }
        
        /// <summary>
        /// The table name of the child object in the schema.
        /// </summary>
        public String TableName { get; private set; }

        /// <summary>
        /// The database name of the child object in the schema.
        /// </summary>
        public String DatabaseName { get; private set; }

        /// <summary>
        /// The schema name of the child object in the schema.
        /// </summary>
        public String SchemaName { get; set; }

        /// <summary>
        /// Whether or not to use the (nolock) join hint when joining the child object's table in the schema.
        /// </summary>
        public Boolean UseNoLock { get; set; }

        /// <summary>
        /// The fully qualified table name (including database name, schema name and table name) for the child object in the schema.
        /// </summary>
        public String FullyQualifiedTableName { get { return String.Format("{0}.{1}.{2}", this.DatabaseName, this.SchemaName, this.TableName); } }

        /// <summary>
        /// Whether or not to use a join that ignores nulls, or one that includes nulls.
        /// </summary>
        public String JoinType { get { return "INNER"; } }

        /// <summary>
        /// A check to make sure there are no self-referencing cycles in the parent-child graph.
        /// </summary>
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
    }
}
