using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier
{
    public class DataException : System.Exception
    {
        public DataException() { }
        public DataException(String message, Exception innerException) : base(message, innerException) { }
    }
}
