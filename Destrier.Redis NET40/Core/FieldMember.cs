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
            this.IsNullableType = ReflectionUtil.IsNullableType(this.MemberType);

            if (this.IsNullableType)
                this.UnderlyingType = ReflectionUtil.GetUnderlyingTypeForNullable(this.MemberType);

            _getValue = ReflectionUtil.CompileFieldAccess(fieldInfo);
            _setValue = ReflectionUtil.CompileFieldAssignment(fieldInfo);
        }
    }
}
