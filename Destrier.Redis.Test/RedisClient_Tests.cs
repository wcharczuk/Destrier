using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Destrier.Redis.Test
{
    public class RedisClient_Tests
    {
        const string HOST = "127.0.0.1";

        [Fact]
        public void Connect_Test()
        {
            using (var rc = new RedisClient(HOST))
            {
                Assert.True(rc.Connection.Socket.Connected);
            }
        }

        [Fact]
        public void Set_Get_Test()
        {
            using (var rc = new RedisClient(HOST))
            {
                var key = System.Guid.NewGuid().ToString("N");
                try
                {
                    var value = "TEST_VALUE";
                    var didSet = rc.Set(key, value);

                    Assert.True(didSet);

                    var returned_value = rc.Get(key);
                    Assert.Equal(value, returned_value);
                }
                finally
                {
                    rc.Remove(key);
                }
            }
        }

        [Fact]
        public void MultiSet_Test()
        {
            using (var rc = new RedisClient(HOST))
            {
                var values = new Dictionary<string, string>() {
                    { System.Guid.NewGuid().ToString("N"), "TEST_VALUE" },
                    { System.Guid.NewGuid().ToString("N"), "TEST_VALUE" },
                    { System.Guid.NewGuid().ToString("N"), "TEST_VALUE" }
                };
                try
                {
                    rc.MultiSet(values);

                    var value = rc.Get(values.Keys.First());
                    Assert.Equal(value, "TEST_VALUE");
                }
                finally
                {
                    rc.Remove(values.Keys);
                }
            }
        }
    }
}
