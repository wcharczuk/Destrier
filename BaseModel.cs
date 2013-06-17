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
            var members = ReflectionCache.GetColumnMemberStandardizedLookup(thisType);
            foreach (ColumnMember col in members.Values)
            {
                col.SetValue(this, dr.Get(col.Type, col.Name));
            }
        }
    }
}

