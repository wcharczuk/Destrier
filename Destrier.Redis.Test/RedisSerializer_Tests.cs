using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Destrier.Redis.Test
{
    public enum Speciality
    {
        None = 0,
        Foos = 1,
        Bars = 1
    }
    public class MockObject
    {
        public Int32 Id { get; set; }
        public String Name { get; set; }
        public String EmailAddress { get; set; }
        public Speciality Specialty { get; set; }

        public List<String> Tags { get; set; }
    }
    public class RedisSerializer_Tests
    {
        [Fact]
        public void Serialize_Test()
        {
            var mobj = new MockObject() { Id = 2, EmailAddress = "will@foo.com", Specialty = Speciality.Foos, Name = "Will", Tags = new List<String>() { "Stuff", "More Stuff" } };

            var serialized = Core.RedisSerializer.Serialize(mobj);

            Assert.NotNull(serialized);
            Assert.NotEmpty(serialized);
        }
    }
}
