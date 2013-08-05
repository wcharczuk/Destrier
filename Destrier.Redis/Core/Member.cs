using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Destrier.Redis.Core
{
    public abstract class Member : ICloneable
    {
        public Member(MemberInfo mi)
        {
            this.Name = mi.Name;
            this.DeclaringType = mi.DeclaringType;
            this.IsKey = Model.IsKey(mi);
            this.KeyOrder = Model.GetKeyOrder(mi) ?? 0;

            this.IsBinarySerialized = Model.IsBinarySerialized(mi);
        }

        protected Func<Object, Object> _getValue;
        protected Action<Object, Object> _setValue;

        public virtual String Name { get; set; }

        public virtual Boolean IsKey { get; set; }
        public virtual Int32 KeyOrder { get; set; }

        public virtual Boolean IsBinarySerialized { get; set; }

        public virtual Type MemberType { get; set; }
        public virtual Boolean IsNullableType { get; set; }
        public virtual Type UnderlyingType { get; set; }

        public virtual Type DeclaringType { get; set; }

        public virtual Member Parent { get; set; }

        public object GetValue(object instance)
        {
            return _getValue(instance);
        }

        public void SetValue(object instance, object value)
        {
            _setValue(instance, value);
        }

        private string _fullyQualifiedName = null;
        public String FullyQualifiedName
        {
            get
            {
                if (!String.IsNullOrEmpty(_fullyQualifiedName))
                    return _fullyQualifiedName;

                if (Parent != null)
                    _fullyQualifiedName = String.Format("{0}{1}{2}", Parent.FullyQualifiedName, Model.KeySeparator, Name);
                else
                    _fullyQualifiedName = Name;

                return _fullyQualifiedName;
            }
        }

        public override int GetHashCode()
        {
            return FullyQualifiedName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var them = obj as Member;
            if (them == null) return false;

            return them.FullyQualifiedName.Equals(this.FullyQualifiedName);
        }

        public override string ToString()
        {
            return this.FullyQualifiedName;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
