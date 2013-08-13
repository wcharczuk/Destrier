using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Destrier.Redis.Core;
using Xunit;

namespace Destrier.Redis.Test
{
    public class RedisSerializer_Tests
    {
        public void BinarySerialize_Test()
        {
            RedisContext.AddHost("Default", "127.0.0.1");

            var mobj = new MockObject() { Id = 2, EmailAddress = "will@foo.com", Specialty = Speciality.Foos, Name = "Will", Tags = new List<String>() { "Stuff", "More Stuff" } };
            var id = System.Guid.NewGuid().ToString("N");
            var key = String.Format("tracking:{0}", id);

            using (var rc = RedisContext.GetClient())
            {
                var reply_mobj = rc.BinaryDeserializeObject<MockObject>(key);
                Assert.Equal(mobj.EmailAddress, reply_mobj.EmailAddress);
            }
        }

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
