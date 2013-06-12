using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier
{
    public class SchemaMetadataException : Exception
    {
        public SchemaMetadataException() : base() { }
        public SchemaMetadataException(String message) : base(message) { }
    }
}
