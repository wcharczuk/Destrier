using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier
{
    public class InvalidColumnDataException : Exception
    {
        public InvalidColumnDataException() : base() { }
        public InvalidColumnDataException(String message) : base(message: message) { }
        public InvalidColumnDataException(ColumnAttribute column, object value)
        {
            this.Column = column;
            this.Value = value;
        }

        public ColumnAttribute Column { get; set; }
        public object Value { get; set; }
    }
}
