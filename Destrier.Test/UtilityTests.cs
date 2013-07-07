using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Destrier;

namespace Destrier.Test
{
    public class UtilityTests
    {
        [Fact]
        public void Parallel_Test()
        {
            var counter = 0;
            var counter2 = 0;
            Parallel.Execute(() => { counter = counter + 1; }, () => { counter2 = counter2 + 1; });
            Assert.NotEqual(0, counter);
            Assert.NotEqual(0, counter2);
        }

        [Fact]
        public void DisplayIfNull()
        {
            object nullObj = null;
            object obj = new object();
            Assert.Equal("-", nullObj.DisplayIfNull("-"));
            Assert.NotEqual("-", obj.DisplayIfNull("-"));
        }

        [Fact]
        public void AgileObject_Test()
        {
            var obj = new { id = 1, name = "name" };
            var agile = obj.ToDynamic();
            Assert.NotNull(agile);
            Assert.NotEmpty(agile);
        }
    }
}
