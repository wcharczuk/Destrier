using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier
{
    public interface IGet<T> where T : BaseModel
    {
        T Get(dynamic parameters = null);
    }
}