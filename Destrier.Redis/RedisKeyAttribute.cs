using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Destrier.Redis
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class RedisKeyAttribute : Attribute
    {
        public RedisKeyAttribute() : base() { }

        public RedisKeyAttribute(Int32 order) : base()
        {
            this.Order = order;
        }

        public Int32 Order { get; set; }
    }
}
