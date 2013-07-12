using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier
{
    public interface IGetMany<T>
    {
        IEnumerable<T> GetMany();
    }
}