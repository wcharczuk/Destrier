using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace Destrier
{
    public interface IPopulate
    {
        void Populate(IndexedSqlDataReader dr);
    }
}
