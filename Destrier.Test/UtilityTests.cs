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
            Book nullObject = null;
            Assert.Null(nullObject.ToDynamic());

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
            var empty = String.Empty;
            Assert.Equal(empty, empty.ToNumericsOnly());

            var str = "not123only";
            var numerics = str.ToNumericsOnly();
            Assert.NotNull(numerics);
            Assert.NotEmpty(numerics);
            Assert.Equal("123", numerics);
        }

        [Fact]
        public void NumericsOnly_100Plus_Test()
        {
            StringBuilder sb = new StringBuilder();
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 100; y++)
                {
                    if (x % 2 == 0)
                    {
                        sb.Append("a");
                    }
                    else
                    {
                        sb.Append("1");
                    }
                }
            }

            var numericsOnly = sb.ToString().ToNumericsOnly();
            Assert.True(numericsOnly.Contains("1"));
            Assert.False(numericsOnly.Contains("a"));
        }

        [Fact]
        public void NonNumericsOnly_Test()
        {
            var empty = String.Empty;
            Assert.Equal(empty, empty.ToNonNumericsOnly());

            var str = "not123only";
            var numerics = str.ToNonNumericsOnly();
            Assert.NotNull(numerics);
            Assert.NotEmpty(numerics);
            Assert.Equal("notonly", numerics);
        }

        [Fact]
        public void NonNumericsOnly_100Plus_Test()
        {
            StringBuilder sb = new StringBuilder();
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 100; y++)
                {
                    if (x % 2 == 0)
                    {
                        sb.Append("a");
                    }
                    else
                    {
                        sb.Append("1");
                    }
                }
            }

            var numericsOnly = sb.ToString().ToNonNumericsOnly();
            Assert.False(numericsOnly.Contains("1"));
            Assert.True(numericsOnly.Contains("a"));
        }

        [Fact]
        public void ToLowerCaseFirstLetter_Test()
        {
            var empty = String.Empty;
            Assert.Equal(empty, empty.ToLowerCaseFirstLetter());

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

            var validList = "someone@somewhere.com, someoneElse@somewhere.com";

            Assert.True(validList.IsValidEmailAddress());
        }

        [Fact]
        public void SetPropertiesFrom_Test()
        {
            Book nullBook = null;
            Book setFromNull = new Book();
            setFromNull.SetPropertiesFrom(nullBook);


            var book1 = new Book() { Id = 1, Title = "Book 1", Notes = "This is Book 1", Year = 2013 };
            var book2 = new Book();
            book2.SetPropertiesFrom(book1);

            Assert.Equal(book1.Id, book2.Id);
            Assert.Equal(book1.Title, book2.Title);
            Assert.Equal(book1.Notes, book2.Notes);
            Assert.Equal(book1.Year, book2.Year);
        }

        [Fact]
        public void DBNullCoalese_Test()
        {
            String nullString = null;
            String emptyString = String.Empty;

            DateTime defaultDateTime = default(DateTime);

            Assert.True(nullString.DBNullCoalese() is DBNull);
            Assert.True(emptyString.DBNullCoalese() is DBNull);
            Assert.True(defaultDateTime.DBNullCoalese() is DBNull);

            Int32 notNullOrDefault = 1;
            Assert.False(notNullOrDefault.DBNullCoalese() is DBNull);
        }
    }
}
