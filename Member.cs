﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Destrier
{
    public abstract class Member
    {
        public Member() { }

        public Member(Type type)
        {
            this.Name = type.Name;
            this.Type = type;
        }

        public Member(PropertyInfo pi)
        {
            this.Name = pi.Name;
            this.Type = pi.PropertyType;
            this.DeclaringType = pi.DeclaringType;
            this.Property = pi;

            this._setValueAction = pi.GetSetMethod();
            this._getValueAction = pi.GetGetMethod();
        }

        public Member(Member member)
        {
            this.SetPropertiesFrom(member);
        }

        public virtual String Name { get; set; }
        public virtual Type Type { get; set; }
        public virtual Type DeclaringType { get; set; }
        public virtual PropertyInfo Property { get; set; }

        public virtual String TableAlias { get; set; }
        public virtual String OutputTableName { get; set; }

        public virtual Member Root { get; set; }
        public virtual Member Parent { get; set; }

        private MethodInfo _getValueAction = null;
        public virtual Object GetValue(object instance)
        {
            return _getValueAction.Invoke(instance, null);
        }

        private MethodInfo _setValueAction = null;
        public virtual void SetValue(object instance, object value)
        {
            _setValueAction.Invoke(instance, new object[] { value });
        }

        private String _fullyQualifiedName = null;
        public String FullyQualifiedName
        {
            get 
            {
                if (!String.IsNullOrEmpty(_fullyQualifiedName))
                    return _fullyQualifiedName;

                if (Parent != null)
                    _fullyQualifiedName = String.Format("{0}.{1}", Parent.FullyQualifiedName, Name);
                else
                    _fullyQualifiedName =  String.Format("{1}", this.DeclaringType.Name, Name);

                return _fullyQualifiedName;
            }
        }

        public virtual Boolean HasCycle
        {
            get
            {
                return ParentAny(m => m.Type.Equals(this.Type));
            }
        }

        public virtual Boolean ParentAny(Func<Member, Boolean> test)
        {
            if (this.Parent == null && this.Root != null)
                return test(this.Root);
            else if (test(this.Parent))
                return true;
            else
                return this.Parent.ParentAny(test);
        }

        public override int GetHashCode()
        {
            return FullyQualifiedName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Member)) return false;

            return (obj as Member).FullyQualifiedName == this.FullyQualifiedName;
        }

        public override string ToString()
        {
            return this.FullyQualifiedName;
        }
    }
}
