using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier
{
    public class ColumnNullabilityException : Exception
    {
        public ColumnNullabilityException() : base() { }
        public ColumnNullabilityException(String message) : base(message: message) { }
    }
}