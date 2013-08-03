using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Destrier.Redis.Core
{
    public class FieldMember : Member
    {
        public FieldMember(FieldInfo fieldInfo)
        {
            this.Name = fieldInfo.Name;
            this.DeclaringType = fieldInfo.DeclaringType;
            this.MemberType = fieldInfo.FieldType;
            this.IsNullableType = ReflectionCache.IsNullableType(this.MemberType);

            if (this.IsNullableType)
                this.UnderlyingType = ReflectionCache.GetUnderlyingTypeForNullable(this.MemberType);

            _getValue = ReflectionCache.CompileFieldAccess(fieldInfo);
            _setValue = ReflectionCache.CompileFieldAssignment(fieldInfo);
        }
    }
}
