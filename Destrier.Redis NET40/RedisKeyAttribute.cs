using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier.Redis
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RedisKeyAttribute : Attribute
    {
        public RedisKeyAttribute():base() { }
    }
}
