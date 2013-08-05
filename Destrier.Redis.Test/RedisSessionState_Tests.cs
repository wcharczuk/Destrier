using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Destrier.Redis.Core;
using Destrier.Redis.SessionState;
using Xunit;

namespace Destrier.Redis.Test
{
    public class RedisSessionState_Tests
    {
        [Fact]
        public void RedisSessionStateItem_Tests()
        {
            RedisContext.DefaultHost = "127.0.0.1";

            var sessionState = new RedisSessionStateItem();
            sessionState.SessionId = System.Guid.NewGuid().ToString("N").ToLower();
            sessionState.ApplicationName = "TestApplication";
            sessionState.Created = DateTime.UtcNow;
            sessionState.SesssionItems = new MockObject() { Id = 2, EmailAddress = "will@foo.com", Specialty = Speciality.Foos, Name = "Will", Tags = new List<String>() { "Stuff", "More Stuff" } };

            using(var rc = RedisContext.GetClient())
            {
                rc.Serialize(sessionState);

                var deserialized = rc.Deserialize<RedisSessionStateItem>(Model.GetKey(sessionState));

                Assert.Equal(sessionState.SessionId, deserialized.SessionId);
                Assert.Equal((sessionState.SesssionItems as MockObject).EmailAddress, (deserialized.SesssionItems as MockObject).EmailAddress);

                rc.Update(sessionState, s => s.Locked, true);
                rc.Update(sessionState, s => s.LockDate, DateTime.UtcNow);
                deserialized = rc.Deserialize<RedisSessionStateItem>(Model.GetKey(sessionState));

                Assert.True(deserialized.Locked);
                Assert.True(deserialized.LockDate != default(DateTime));

                rc.Delete(sessionState);
            }
        }
    }
}
