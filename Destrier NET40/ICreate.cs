using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace Destrier
{
    public interface ICreate
    {
        void Create(String connectionName);
    }
}
