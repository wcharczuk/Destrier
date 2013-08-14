using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Destrier.Redis.Test
{
    public class RedisBaseTest
    {
        protected static readonly RedisHostInfo HostInfo = new RedisHostInfo()
        {
            Host = "127.0.0.1"
        };

        protected static readonly String MyKey = "MyKey";
        protected static readonly String MyKey2 = "MyKey2";

        protected static readonly MockObject MockObject = new Test.MockObject()
        {
            EmailAddress = "test@foo.com"
            , Id = 101
            , Name = "Test Object"
            , Specialty = Speciality.Bars
        };

        protected static readonly MockObject MockObject2 = new MockObject()
        {
            EmailAddress = "nottest@foo.net"
            ,  Id = 202
            , Name = "Not Test Object"
            , Specialty = Speciality.Foos
        };
    }
}
