using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier
{
    public class ColumnMissingException : Exception
    {
        public ColumnMissingException(String message) : base(message) { }
    }
}
