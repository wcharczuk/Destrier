using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier.Redis.Core
{
    public enum RedisKeyType
    {
        None = 0,
        String = 1,
        Set = 2,
        List = 3
    }
}
