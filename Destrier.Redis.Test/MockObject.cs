using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Destrier.Redis.Test
{
    public enum Speciality
    {
        None = 0,
        Foos = 1,
        Bars = 1
    }

    [Serializable]
    public class MockObject
    {
        public Int32 Id { get; set; }
        public String Name { get; set; }
        public String EmailAddress { get; set; }
        public Speciality Specialty { get; set; }

        public List<String> Tags { get; set; }
    }
}
