using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier
{
    public interface IGet<T> where T : new()
    {
        T Get(dynamic parameters);
    }
}