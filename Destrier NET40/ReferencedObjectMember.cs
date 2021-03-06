﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Destrier
{
    /// <summary>
    /// Represents a referenced object property, also conceptually a join.
    /// </summary>
    public class ReferencedObjectMember : Member, ICloneable
    {
        public ReferencedObjectMember(PropertyInfo pi) : base(pi)
        {
            this.ReferencedObjectAttribute = ModelReflection.ReferencedObjectAttribute(pi);
            this.ReferencedColumnMember = Model.ColumnMemberForPropertyName(DeclaringType, ReferencedObjectAttribute.PropertyName).Clone() as ColumnMember;
            this.ReferencedColumnIsNullable = ReflectionHelper.IsNullableType(ReferencedColumnProperty.PropertyType) || ReferencedColumnAttribute.CanBeNull;

            if (this.IsLazy)
            {
                this.TableName = Model.TableName(this.UnderlyingGenericType);
                this.DatabaseName = Model.DatabaseName(this.UnderlyingGenericType);
                this.SchemaName = Model.SchemaName(this.UnderlyingGenericType);
                this.UseNoLock = Model.UseNoLock(this.UnderlyingGenericType);
            }
            else
            {
                this.TableName = Model.TableName(this.Type);
                this.DatabaseName = Model.DatabaseName(this.Type);
                this.SchemaName = Model.SchemaName(this.Type);
                this.UseNoLock = Model.UseNoLock(this.Type);
            }
        }

        public ReferencedObjectAttribute ReferencedObjectAttribute { get; set; }

        public ColumnMember ReferencedColumnMember { get; set; }

        public PropertyInfo ReferencedColumnProperty { get { return ReferencedColumnMember.Property; } }
        public ColumnAttribute ReferencedColumnAttribute { get { return ReferencedColumnMember.ColumnAttribute; } }

        public Boolean ReferencedColumnIsNullable { get; set; }

        public String TableName { get; private set; }
        public String DatabaseName { get; private set; }
        public String SchemaName { get; set; }
        public Boolean UseNoLock { get; set; }

        public String JoinType { get { return ReferencedColumnIsNullable ? "LEFT" : "INNER"; } }

        public String FullyQualifiedTableName { get { return String.Format("{0}.{1}.{2}", this.DatabaseName, this.SchemaName, this.TableName); } }

        public override object Clone()
        {
            var copy = this.MemberwiseClone() as ReferencedObjectMember;
            copy.ReferencedColumnMember = this.ReferencedColumnMember.Clone() as ColumnMember;
            return copy;
        }
    }
}
