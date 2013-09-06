using Destrier.Redis.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Destrier.Redis.Test
{
    public class RedisClient_Tests : RedisBaseTest
    {
        [Fact]
        public void Connect_Test()
        {
            using (var rc = new RedisClient(HostInfo))
            {
                Assert.True(rc.Connection.Socket.Connected);
                Assert.True(rc.Db.HasValue && rc.Db == 2);
            }
        }

        [Fact]
        public void Keys_Test()
        {
            using (var rc = new RedisClient(HostInfo))
            {
                var key = System.Guid.NewGuid().ToString("N");

                var value = "TEST_VALUE";
                var didSet = rc.Set(key, value);

                Assert.True(didSet);

                var returned_value = rc.Get(key);
                Assert.Equal(value, returned_value);
                Assert.True(rc.Remove(key));
            }
        }

        [Fact]
        public void MultiSet_Test()
        {
            using (var rc = new RedisClient(HostInfo))
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
                    rc.Remove(values.Keys.ToArray());
                }
            }
        }

        [Fact]
        public void Sets_Test()
        {
            using (var rc = new RedisClient(HostInfo))
            {
                //SADD test :: moobars, foobars
                //SCARD test => 2
                //SADD test2 :: moobars, barfoos
                //SUNION test test2 => moobars, foobars, barfoos
                //SETMEMBERS test => moobars, foobars
                //SADD letters :: <all letters in alphabet, 0->27>
                //SRANDMEMBER letters

                rc.SetAdd("set_test", "moobars", "foobars");
                rc.SetAdd("set_test2", "moobars", "barfoos", "woozles");

                var size = rc.SetCardinality("set_test");
                var test2_size = rc.SetCardinality("set_test2");
                Assert.Equal(2, size);
                Assert.Equal(3, test2_size);

                var union = rc.SetUnion("set_test", "set_test2").ToList();
                Assert.Equal(4, union.Count());
                Assert.True(union.Any(v => v.Equals("barfoos")));

                rc.SetRemove("set_test2", "woozles");

                test2_size = rc.SetCardinality("test2");
                Assert.Equal(2, size);

                var test3_size = rc.SetUnionStore("set_test3", "set_test", "set_test2");
                Assert.Equal(3, test3_size);

                var random = rc.SetRandomMember("set_test");

                Assert.NotNull(random);

                rc.Remove("set_test");
                rc.Remove("set_test2");
                rc.Remove("set_test3");
            }
        }

        [Fact]
        public void SortedSets_Test()
        {
            using (var rc = new RedisClient(HostInfo))
            {
                rc.SortedSetAdd("myzset", 1, "one");
                rc.SortedSetAdd("myzset", 2, "two");
                rc.SortedSetAdd("myzset", 3, "three");

                var size = rc.SortedSetCardinality("myzset");

                Assert.Equal(3, size);

                var items = rc.SortedSetRange("myzset", 0, -1);
                Assert.NotNull(items);
                Assert.NotEmpty(items);
                Assert.Equal(3, items.Count());
                Assert.True(items.First() == "one");

                rc.SortedSetRemove("myzset", "one");

                size = rc.SortedSetCardinality("myzset");

                Assert.Equal(2, size);

                rc.FlushDb();
            }
        }
    }
}
