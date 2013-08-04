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

    [Serializable]
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
            RedisContext.DefaultHost = "127.0.0.1";

            var mobj = new MockObject() { Id = 2, EmailAddress = "will@foo.com", Specialty = Speciality.Foos, Name = "Will", Tags = new List<String>() { "Stuff", "More Stuff" } };
            var id = System.Guid.NewGuid().ToString("N");
            var key = String.Format("tracking:{0}", id);
            RedisBinarySerializer.Serialize(key, mobj);

            var reply_mobj = RedisBinarySerializer.Deserialize<MockObject>(key);

            Assert.Equal(mobj.EmailAddress, reply_mobj.EmailAddress);
        }
    }
}
