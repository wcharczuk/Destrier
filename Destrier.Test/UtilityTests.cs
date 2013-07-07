using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Destrier;
using Destrier.Extensions;

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
        public void AgileObject_Test()
        {
            var obj = new { id = 1, name = "name" };
            var agile = obj.ToDynamic();
            Assert.NotNull(agile);
            Assert.NotEmpty(agile);
        }

        [Fact]
        public void TitleCase_Test()
        {
            var str = "not title case";
            var title_case = str.ToTitleCase();
            Assert.NotNull(title_case);
            Assert.NotEmpty(title_case);
            Assert.Equal("Not Title Case", title_case);
        }

        [Fact]
        public void NumericsOnly_Test()
        {
            var str = "not123only";
            var numerics = str.ToNumericsOnly();
            Assert.NotNull(numerics);
            Assert.NotEmpty(numerics);
            Assert.Equal("123", numerics);
        }

        [Fact]
        public void NonNumericsOnly_Test()
        {
            var str = "not123only";
            var numerics = str.ToNonNumericsOnly();
            Assert.NotNull(numerics);
            Assert.NotEmpty(numerics);
            Assert.Equal("notonly", numerics);
        }

        [Fact]
        public void ToLowerCaseFirstLetter_Test()
        {
            var str = "UPPERCASE";
            var lowerFirst = str.ToLowerCaseFirstLetter();
            Assert.NotNull(lowerFirst);
            Assert.NotEmpty(lowerFirst);
            Assert.Equal("uPPERCASE", lowerFirst);
        }

        [Fact]
        public void IsValidEmailAddress_Test()
        {
            var valid = "someone@somewhere.com";
            var notValid = "notvalid";

            Assert.True(valid.IsValidEmailAddress());
            Assert.False(notValid.IsValidEmailAddress());
        }
    }
}
