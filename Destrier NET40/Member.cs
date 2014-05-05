using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Destrier.Extensions;

namespace Destrier
{
    public abstract class Member : ICloneable
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

            this.IsNullableType = ReflectionHelper.IsNullableType(this.Type);
            if (IsNullableType)
                this.UnderlyingGenericType = ReflectionHelper.GetUnderlyingTypeForNullable(this.Type);

            this.IsLazy = ReflectionHelper.IsLazy(this.Type);
            if (this.IsLazy)
                this.UnderlyingGenericType = ReflectionHelper.GetUnderlyingTypeForLazy(this.Type);

            this.DeclaringType = pi.DeclaringType;
            this.Property = pi;

            this._setValueCompiled = ReflectionHelper.GetSetAction(pi);
            this._getValueMethod = pi.GetGetMethod();
        }

        public Member(Member member)
        {
            this.SetPropertiesFrom(member);
        }

        public virtual String Name { get; set; }
        public virtual Type Type { get; set; }
        public virtual Boolean IsNullableType { get; set; }
        public virtual Type UnderlyingGenericType { get; set; }
        public virtual Type DeclaringType { get; set; }
        public virtual PropertyInfo Property { get; set; }

        public virtual Boolean IsLazy { get; set; }

        public virtual String TableAlias { get; set; }
        public virtual String OutputTableName { get; set; }
        
        public virtual Member Root { get; set; }
        public virtual Member Parent { get; set; }

        private MethodInfo _getValueMethod = null;
        public virtual Object GetValue(object instance)
        {
            return _getValueMethod.Invoke(instance, null);
        }

        private readonly Action<Object, Object> _setValueCompiled = null;
        public virtual void SetValue(object instance, object value)
        {
            _setValueCompiled(instance, value);
        }

        public String FullyQualifiedName
        {
            get
            {
                if (Parent != null)
                    return String.Format("{0}.{1}", Parent.FullyQualifiedName, Name);
                else
                    return this.Name;
            }
        }

        public virtual Boolean HasCycle
        {
            get
            {
                return AnyParent(m => m.Type.Equals(this.Type));
            }
        }

        public virtual Boolean AnyParent(Func<Member, Boolean> test)
        {
            if (this.Parent == null && this.Root != null)
                return test(this.Root);
            else if (this.Parent != null && test(this.Parent))
                return true;
            else if (this.Parent != null)
                return this.Parent.AnyParent(test);
            else
                return false;
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

        public virtual object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}