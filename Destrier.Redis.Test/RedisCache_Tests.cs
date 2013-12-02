using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Destrier.Redis.Cache;
using Xunit;

namespace Destrier.Redis.Test
{
    public class RedisCache_Tests : RedisBaseTest
    {
        [Fact]
        public void Connect_Test()
        {
            RedisCache.Current.Connect(HostInfo);
            Assert.True(RedisCache.Current.Connection.IsConnected);
        }

        [Fact]
        public void AddItem_Test()
        {
            RedisCache.Current.Connect(HostInfo);
            RedisCache.Current.Add(MyKey, MockObject);
            var retrieved = RedisCache.Current[MyKey] as MockObject;
            Assert.NotNull(retrieved);
            Assert.Equal(MockObject.EmailAddress, retrieved.EmailAddress);
        }

        [Fact]
        public void AddItem_WithExpiration_Test()
        {
            RedisCache.Current.Connect(HostInfo);
            RedisCache.Current.Add(MyKey, MockObject, slidingExpiration: TimeSpan.FromSeconds(1));
            var retrieved = RedisCache.Current[MyKey] as MockObject;
            Assert.NotNull(retrieved);
            Assert.Equal(MockObject.EmailAddress, retrieved.EmailAddress);
            System.Threading.Thread.Sleep(2000);
            retrieved = RedisCache.Current[MyKey] as MockObject;
            Assert.Null(retrieved);
        }

        [Fact]
        public void RemoveItem_Test()
        {
            RedisCache.Current.Connect(HostInfo);
            RedisCache.Current.Add(MyKey, MockObject);
            var retrieved = RedisCache.Current[MyKey] as MockObject;
            Assert.NotNull(retrieved);
            RedisCache.Current.Remove(MyKey);
            retrieved = RedisCache.Current[MyKey] as MockObject;
            Assert.Null(retrieved);
        }

        [Fact]
        public void Keys_Test()
        {
            RedisCache.Current.Connect(HostInfo);
            RedisCache.Current.Add(MyKey, MockObject);
            var keys = RedisCache.Current.Keys.ToList();

            Assert.NotEmpty(keys);
            Assert.Equal(MyKey, keys.First());

            RedisCache.Current.Remove(MyKey);

            keys = RedisCache.Current.Keys.ToList();
            Assert.False(keys.Any(k => k.Equals(MyKey)));
        }

        [Fact]
        public void ContainsKey_Test()
        {
            RedisCache.Current.Connect(HostInfo);
            RedisCache.Current.Add(MyKey, MockObject);
            Assert.True(RedisCache.Current.ContainsKey(MyKey));
            RedisCache.Current.Remove(MyKey);
            Assert.False(RedisCache.Current.ContainsKey(MyKey));
        }

        [Fact]
        public void Set_Test()
        {
            RedisCache.Current.Connect(HostInfo);
            RedisCache.Current.Add(MyKey, MockObject);
            RedisCache.Current.Set(MyKey, MockObject2);

            var retrieved = RedisCache.Current[MyKey] as MockObject;
            Assert.Equal(MockObject2.EmailAddress, retrieved.EmailAddress);
            RedisCache.Current.Remove(MyKey);

            Assert.Throws(typeof(KeyNotFoundException), () =>
            {
                RedisCache.Current.Set(MyKey, MockObject);
            });
        }

        [Fact]
        public void SetOrAdd_Test()
        {
            RedisCache.Current.Connect(HostInfo);
            
            RedisCache.Current.Remove(MyKey);

            RedisCache.Current.SetOrAdd(MyKey, MockObject);

            var retrieved = RedisCache.Current[MyKey] as MockObject;
            Assert.Equal(MockObject.EmailAddress, retrieved.EmailAddress);

            RedisCache.Current.SetOrAdd(MyKey, MockObject2);
            retrieved = RedisCache.Current[MyKey] as MockObject;
            Assert.Equal(MockObject2.EmailAddress, retrieved.EmailAddress);
        }

        [Fact]
        public void Count_Test()
        {
            RedisCache.Current.Connect(HostInfo);
            RedisCache.Current.SetOrAdd(MyKey, MockObject);
            Assert.True(RedisCache.Current.Count >= 1);
            RedisCache.Current.Clear(); //sort of testing this at the same time.
            Assert.True(RedisCache.Current.Count == 0);
        }

        [Fact]
        public void Touch_Test()
        {
            RedisCache.Current.Connect(HostInfo);
            RedisCache.Current.Add(MyKey, MockObject, slidingExpiration: TimeSpan.FromSeconds(1));
            var retrieved = RedisCache.Current[MyKey] as MockObject;
            Assert.NotNull(retrieved);
            RedisCache.Current.Touch(MyKey, newSlidingExpiration: TimeSpan.FromSeconds(1));

        }

        [Fact]
        public void Enumerator_Test()
        {
            RedisCache.Current.Connect(HostInfo);
            RedisCache.Current.Clear();
            RedisCache.Current.SetOrAdd(MyKey, MockObject);
            RedisCache.Current.SetOrAdd(MyKey2, MockObject2);

            int count = 0;
            foreach (var kvp in RedisCache.Current)
            {
                count++;
            }
            Assert.True(count == 2);
        }
    }
}
