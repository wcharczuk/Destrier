using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Destrier.Redis
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class RedisBinarySerializeAttribute : Attribute
    {
        public RedisBinarySerializeAttribute() : base() { }
    }
}
