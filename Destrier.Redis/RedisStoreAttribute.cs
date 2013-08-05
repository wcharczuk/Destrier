using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Destrier.Redis
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class RedisStoreAttribute : Attribute
    {
        public RedisStoreAttribute() : base() { }
        public RedisStoreAttribute(String keyPrefix) : base() { this.KeyPrefix = keyPrefix; }
        public RedisStoreAttribute(String keyPrefix, Int32 db)
        {
            this.KeyPrefix = keyPrefix;
            this.DB = db;
        }

        public String KeyPrefix { get; set; }
        public Int32 DB { get; set; }
    }
}
