using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Destrier.Redis.Core
{
    public class PropertyMember : Member
    {
        public PropertyMember(PropertyInfo propertyInfo)
        {
            this.Name = propertyInfo.Name;
            this.DeclaringType = propertyInfo.DeclaringType;
            this.MemberType = propertyInfo.PropertyType;
            this.IsNullableType = ReflectionUtil.IsNullableType(this.MemberType);

            if (this.IsNullableType)
                this.UnderlyingType = ReflectionUtil.GetUnderlyingTypeForNullable(this.MemberType);

            _getValue = ReflectionUtil.CompilePropertyAccess(propertyInfo);
            _setValue = ReflectionUtil.CompilePropertyAssignment(propertyInfo);
        }
    }
}
