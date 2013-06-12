using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Reflection;
using System.Dynamic;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Destrier
{
    [Serializable]
    public abstract class BaseModel : IPopulate
    {
        public BaseModel() { }

        public delegate void DatabaseActionHandler(object sender, EventArgs e);

        [field: NonSerialized]
        protected event DatabaseActionHandler PreCreate;
        [field: NonSerialized]
        protected event DatabaseActionHandler PostCreate;

        [field: NonSerialized]
        protected event DatabaseActionHandler PreUpdate;
        [field: NonSerialized]
        protected event DatabaseActionHandler PostUpdate;

        [field: NonSerialized]
        protected event DatabaseActionHandler PreRemove;
        [field: NonSerialized]
        protected event DatabaseActionHandler PostRemove;

        public void OnPreCreate(EventArgs e)
        {
            if (PreCreate != null)
                PreCreate(this, e);
        }
        public void OnPostCreate(EventArgs e)
        {
            if (PostCreate != null)
                PostCreate(this, e);
        }

        public void OnPreUpdate(EventArgs e)
        {
            if (PreUpdate != null)
                PreUpdate(this, e);
        }
        public void OnPostUpdate(EventArgs e)
        {
            if (PostUpdate != null)
                PostUpdate(this, e);
        }

        public void OnPreRemove(EventArgs e)
        {
            if (PreRemove != null)
                PreRemove(this, e);
        }
        public void OnPostRemove(EventArgs e)
        {
            if (PostRemove != null)
                PostRemove(this, e);
        }

        public virtual void Populate(IndexedSqlDataReader dr)
        {
            var thisType = this.GetType();
            foreach (PropertyInfo pi in Model.Columns(thisType))
            {
                ColumnAttribute ca = Model.ColumnAttribute(pi);
                String columnName = Model.ColumnName(pi);
                Boolean checkIfDbNull = ca.CanBeNull;

                pi.SetValue(this, dr.Get(pi.PropertyType, columnName), null);
            }
        }

        /// <summary>
        /// This gets called when we read an object from the database.
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="columnsCanBeMissing"></param>
        public virtual void PopulateFullResults(IndexedSqlDataReader dr, Member rootMember = null, ReferencedObjectMember parentMember = null, Dictionary<Type, Dictionary<Object, Object>> objectLookups = null)
        {
            var thisType = this.GetType();
            var members = Model.ColumnMembers(thisType);

            if (Model.HasReferencedObjects(thisType))
            {
                foreach (ColumnMember col in members.Values)
                {
                    var columnName = col.Name;
                    if (parentMember != null)
                        columnName = String.Format("{0}.{1}", parentMember.FullyQualifiedName, col.Name);

                    col.SetValue(this, dr.Get(col.Type, columnName));
                }

                rootMember = rootMember ?? new RootMember(thisType);
                foreach (ReferencedObjectMember rom in Model.Members(thisType, rootMember, parentMember).Where(m => m is ReferencedObjectMember && !m.ParentAny(p => p is ChildCollectionMember)))
                {
                    var type = rom.Type;
                    var newObject = ReflectionCache.GetNewObject(type);
                    (newObject as BaseModel).PopulateFullResults(dr, rootMember, rom);
                    rom.Property.SetValue(this, newObject);

                    if (objectLookups != null)
                    {
                        if (!objectLookups.ContainsKey(rom.Type))
                        {
                            objectLookups.Add(rom.Type, new Dictionary<Object, Object>());
                        }
                        var pkv = Model.InstancePrimaryKeyValue(rom.Type, newObject);
                        if (pkv != null && !objectLookups[rom.Type].ContainsKey(pkv))
                        {
                            objectLookups[rom.Type].Add(pkv, newObject);
                        }
                    }
                }
            }
            else
            {
                for (int x = 0; x < dr.ColumnIndexMap.Length; x++)
                {
                    var name = dr.GetName(x);//dr.ColumnIndexMap[x];
                    if (members.ContainsKey(name))
                    {
                        var member = members[name];
                        var value = dr.Get(member.Type, x);
                        member.SetValue(this, value);
                    }
                }
            }

            if (Model.HasChildCollections(thisType) && objectLookups != null)
            {
                if (!objectLookups.ContainsKey(thisType))
                {
                    objectLookups.Add(thisType, new Dictionary<Object, Object>());
                }
                var pkv = Model.InstancePrimaryKeyValue(thisType, this);
                if (pkv != null && !objectLookups[thisType].ContainsKey(pkv))
                {
                    objectLookups[thisType].Add(pkv, this);
                }
            }
        }
    }
}

