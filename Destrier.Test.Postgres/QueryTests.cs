using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Destrier.Test.Postgres
{
    public class QueryTests : IUseFixture<LibraryContext>
    {
        public void SetFixture(LibraryContext data) { }

        [Fact]
        public void Basic()
        {
            var people = new Query<Person>().Execute();

            Assert.NotNull(people);
            Assert.NotEmpty(people);
        }

        [Fact]
        public void RawQuery()
        {
            var people = new Query<Person>("select * from people").Execute();

            Assert.NotNull(people);
            Assert.NotEmpty(people);
        }

        [Fact]
        public void RawQuery_WithParamters()
        {
            var people = new Query<Person>("select * from people where id = @id", new Dictionary<String, Object>() { { "id", 1 } }).Execute();

            Assert.NotNull(people);
            Assert.NotEmpty(people);
        }

        [Fact]
        public void ReferencedObjects()
        {
            var pages = new Query<Page>().Execute();

            Assert.NotNull(pages);
            Assert.NotEmpty(pages);
            Assert.NotNull(pages.First().Book);
            Assert.NotEqual(0, pages.First().Book.Id);

            Assert.NotNull(pages.First().Book.Author.Name);
            Assert.NotEmpty(pages.First().Book.Author.Name);
        }

        [Fact]
        public void ChildCollections()
        {
            var books = new Query<Book>().Execute();

            Assert.NotNull(books);
            Assert.NotEmpty(books);
            Assert.NotNull(books.First().Chapters);
            Assert.NotEmpty(books.First().Chapters);
            Assert.NotEmpty(books.First().Chapters.First().Title);
        }

        [Fact]
        public void ChildCollections_Include()
        {
            var books = new Query<Book>().Include(b => b.Chapters).Execute();

            Assert.NotNull(books);
            Assert.NotEmpty(books);
            Assert.NotNull(books.First().Chapters);
            Assert.NotEmpty(books.First().Chapters);
            Assert.NotEmpty(books.First().Chapters.First().Title);
        }

        [Fact]
        public void ChildCollections_IncludeByName()
        {
            var query = new Query<Book>().Include("Chapters.Pages");
            var books = query.Execute();

            Assert.NotNull(books);
            Assert.NotEmpty(books);
            Assert.NotNull(books.First().Chapters);
            Assert.NotEmpty(books.First().Chapters);
            Assert.NotEmpty(books.First().Chapters.First().Title);
            Assert.NotNull(books.First(b => b.Id == 1).Chapters.First().Pages);
            Assert.NotEmpty(books.First(b => b.Id == 1).Chapters.First().Pages);
            Assert.NotNull(books.First(b => b.Id == 1).Chapters.First().Pages.First().Text);
            Assert.NotEmpty(books.First(b => b.Id == 1).Chapters.First().Pages.First().Text);
        }

        [Fact]
        public void ChildCollections_IncludeAll()
        {
            var query = new Query<Book>().IncludeAll();
            var books = query.Execute();

            Assert.NotNull(books);
            Assert.NotEmpty(books);
            Assert.NotNull(books.First().Chapters);
            Assert.NotEmpty(books.First().Chapters);
            Assert.NotEmpty(books.First().Chapters.First().Title);
            Assert.NotNull(books.First(b => b.Id == 1).Chapters.First().Pages);
            Assert.NotEmpty(books.First(b => b.Id == 1).Chapters.First().Pages);
            Assert.NotNull(books.First(b => b.Id == 1).Chapters.First().Pages.First().Text);
            Assert.NotEmpty(books.First(b => b.Id == 1).Chapters.First().Pages.First().Text);
        }

        [Fact]
        public void ChildCollections_DontInclude()
        {
            var books = new Query<Book>().DontInclude(b => b.Chapters).Execute();

            Assert.NotNull(books);
            Assert.NotEmpty(books);
            Assert.Null(books.First().Chapters);
        }

        [Fact]
        public void ChildCollections_DontIncludeByName()
        {
            var books = new Query<Book>().DontInclude("Chapters").Execute();

            Assert.NotNull(books);
            Assert.NotEmpty(books);
            Assert.Null(books.First().Chapters);
        }

        [Fact]
        public void ChildCollections_DontIncludeAny()
        {
            var books = new Query<Book>().DontIncludeAny().Execute();

            Assert.NotNull(books);
            Assert.NotEmpty(books);
            Assert.Null(books.First().Chapters);
        }

        [Fact]
        public void Where()
        {
            var books = new Query<Book>().Where(b => b.Id > 2).Execute();

            Assert.NotNull(books);
            Assert.NotEmpty(books);
            Assert.False(books.Any(b => b.Id == 1));
        }

        [Fact]
        public void Where_Referenced()
        {
            var books = new Query<Book>().Where(b => b.Author.Name.Contains("Ernest")).Execute();

            Assert.NotNull(books);
            Assert.NotEmpty(books);
            Assert.True(books.All(b => b.AuthorId == 1));

            books = new Query<Book>().Where(b => b.Author.Name.StartsWith("Ernest")).Execute();

            Assert.NotNull(books);
            Assert.NotEmpty(books);
            Assert.True(books.All(b => b.AuthorId == 1));

            books = new Query<Book>().Where(b => b.Author.Name.EndsWith("Hemingway")).Execute();

            Assert.NotNull(books);
            Assert.NotEmpty(books);
            Assert.True(books.All(b => b.AuthorId == 1));

            books = new Query<Book>().Where(b => b.Author.Name.Trim() == "Ernest Hemingway").Execute();

            Assert.NotNull(books);
            Assert.NotEmpty(books);
            Assert.True(books.All(b => b.AuthorId == 1));

            books = new Query<Book>().Where(b => b.Author.Name.Replace("Ernest", "Bob") == "Bob Hemingway").Execute();

            Assert.NotNull(books);
            Assert.NotEmpty(books);
            Assert.True(books.All(b => b.AuthorId == 1));
        }

        [Fact]
        public void OrderBy()
        {
            var books = new Query<Book>().OrderBy(b => b.Title).Execute();

            Assert.NotNull(books);
            Assert.NotEmpty(books);
            Assert.Equal("Hamlet", books.First().Title);
        }

        [Fact]
        public void OrderByDescending()
        {
            var books = new Query<Book>().OrderByDescending(b => b.Title).Execute();

            Assert.NotNull(books);
            Assert.NotEmpty(books);
            Assert.Equal("Walden", books.First().Title);
        }

        [Fact]
        public void OrderBy_ThenBy()
        {
            var books = new Query<Book>().OrderBy(b => b.Author.Name).ThenOrderBy(b => b.Title).Execute();

            Assert.NotNull(books);
            Assert.NotEmpty(books);
            Assert.Equal("The Old Man and the Sea", books.First().Title);
        }

        [Fact]
        public void OrderBy_ThenByDescending()
        {
            var books = new Query<Book>().OrderBy(b => b.Author.Name).ThenOrderByDescending(b => b.Title).Execute();

            Assert.NotNull(books);
            Assert.NotEmpty(books);
            Assert.Equal("The Old Man and the Sea", books.First().Title);
        }

        [Fact]
        public void Limit()
        {
            var chapters = new Query<Chapter>().Limit(10).Execute();

            Assert.NotNull(chapters);
            Assert.NotEmpty(chapters);
            Assert.True(chapters.Count() == 10);
        }

        [Fact]
        public void Limit_OrderBy()
        {
            var chapters = new Query<Chapter>().Limit(10).OrderByDescending(b => b.Title).Execute();

            Assert.NotNull(chapters);
            Assert.NotEmpty(chapters);
            Assert.True(chapters.Count() == 10);
        }

        [Fact]
        public void Offset()
        {
            var chapters = new Query<Chapter>().Offset(10).Execute();

            Assert.NotNull(chapters);
            Assert.NotEmpty(chapters);
            Assert.True(chapters.First().Id > 10);
        }

        [Fact]
        public void Offset_Limit()
        {
            var chapters = new Query<Chapter>().Offset(10).Limit(10).Execute();

            Assert.NotNull(chapters);
            Assert.NotEmpty(chapters);
            Assert.True(chapters.First().Id > 10);
            Assert.True(chapters.Count() == 10);
        }

        [Fact]
        public void Offset_OrderBy()
        {
            var chapters = new Query<Chapter>().Offset(10).OrderByDescending(c => c.Number).Execute();

            Assert.NotNull(chapters);
            Assert.NotEmpty(chapters);
        }

        [Fact]
        public void Update_Basic()
        {
            var oldPerson = Database.Get<Person>(1);

            new Update<Person>().Set(p => p.Name, "Not Ernest Hemmingway").Where(p => p.Id == 1).Execute();

            var newPerson = Database.Get<Person>(1);
            Assert.NotEqual(oldPerson.Name, newPerson.Name);

            //undo the change.
            Database.Update(oldPerson);
        }

        [Fact]
        public void Update_MassiveDestructiveUpdate()
        {
            new Update<Book>().Set(p => p.Notes, "Same Note For Every Book").Execute();

            var nullTest = new Query<Book>().Where(b => b.Notes == null).Execute();

            Assert.Empty(nullTest);

            new Update<Book>().Set(p => p.Notes, null).Execute();

            nullTest = new Query<Book>().Where(b => b.Notes == null).Execute();

            Assert.NotEmpty(nullTest);
        }
    }
}
