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
            this.IsNullableType = ReflectionCache.IsNullableType(this.MemberType);

            if (this.IsNullableType)
                this.UnderlyingType = ReflectionCache.GetUnderlyingTypeForNullable(this.MemberType);

            _getValue = ReflectionCache.CompilePropertyAccess(propertyInfo);
            _setValue = ReflectionCache.CompilePropertyAssignment(propertyInfo);
        }
    }
}
