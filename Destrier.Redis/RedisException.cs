using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier.Redis
{
    public class RedisException : Exception
    {
        public RedisException(String message) : base(message) { }
    }
}
