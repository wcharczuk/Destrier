using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Destrier.Redis.Core;
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
        public void BinarySerialize_Test()
        {
            RedisContext.DefaultHost = "127.0.0.1";

            var mobj = new MockObject() { Id = 2, EmailAddress = "will@foo.com", Specialty = Speciality.Foos, Name = "Will", Tags = new List<String>() { "Stuff", "More Stuff" } };
            var id = System.Guid.NewGuid().ToString("N");
            var key = String.Format("tracking:{0}", id);

            using (var rc = RedisContext.GetClient())
            {
                var reply_mobj = rc.BinaryDeserialize<MockObject>(key);
                Assert.Equal(mobj.EmailAddress, reply_mobj.EmailAddress);
            }
        }

        [Fact]
        public void DictionaryMap_Test()
        {
            var mobj = new MockObject() { Id = 2, EmailAddress = "will@foo.com", Specialty = Speciality.Foos, Name = "Will", Tags = new List<String>() { "Stuff", "More Stuff" } };
            var dictionary = new Dictionary<Member, Object>();
            ReflectionUtil.MapToDictionary(mobj, dictionary);

            Assert.NotNull(dictionary);
            Assert.NotEmpty(dictionary);
        }
    }
}
