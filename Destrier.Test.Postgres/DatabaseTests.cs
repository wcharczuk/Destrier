using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Destrier.Test.Postgres
{
    public class DatabaseTests : IUseFixture<LibraryContext>
    {
        public void SetFixture(LibraryContext data) { }

        [Fact]
        public void Get()
        {
            var book = Database.Get<Book>(1);
            Assert.NotNull(book);
            Assert.NotEmpty(book.Title);
            Assert.NotNull(book.Author);
            Assert.NotEmpty(book.Chapters);
        }

        [Fact]
        public void All()
        {
            var books = Database.All<Book>();
            Assert.NotNull(books);
            Assert.NotEmpty(books.First().Title);
            Assert.NotNull(books.First().Author);
            Assert.NotEmpty(books.First().Chapters);
        }

        [Fact]
        public void Update()
        {
            var book = Database.Get<Book>(1);
            book.Notes = "New Note.";
            Database.Update(book);

            var newBook = Database.Get<Book>(1);

            Assert.Equal("New Note.", newBook.Notes);
        }

        [Fact]
        public void Create()
        {
            var person = new Person() { Name = "Will" };
            Database.Create(person);
            Assert.NotEqual(0, person.Id);

            var checkPerson = Database.Get<Person>(person.Id);

            Assert.Equal(person.Name, checkPerson.Name);
        }

        [Fact]
        public void Remove()
        {
            var person = new Person() { Name = "Will" };
            Database.Create(person);

            Database.Remove(person);

            var checkPerson = Database.Get<Person>(person.Id);

            Assert.Null(checkPerson);
        }

        [Fact]
        public void RemoveWhere()
        {
            var person = new Person() { Name = "Will" };
            Database.Create(person);

            Database.RemoveWhere<Person>(p => p.Id == person.Id);

            var checkPerson = Database.Get<Person>(person.Id);

            Assert.Null(checkPerson);
        }

        [Fact]
        public void IPreRemove()
        {
            var book = Database.Get<Book>(2);
            Database.Remove(book);

            var checkBook = Database.Get<Book>(2);
            Assert.Null(checkBook);
        }

        [Fact]
        public void IPreCreate()
        {
            var new_book = new Book() { Title = "A New Book", Year = 2013 };
            new_book.Author = new Person() { Name = "Will" };

            Database.Create(new_book);

            var bookCheck = Database.Get<Book>(new_book.Id);
            Assert.NotNull(bookCheck);
            Assert.NotNull(bookCheck.Author);
            Assert.NotEmpty(bookCheck.Author.Name);
            Assert.Equal("Will", bookCheck.Author.Name);
        }

        [Fact]
        public void IPostUpdate()
        {
            var book = Database.Get<Book>(3);
            book.Author.Name = "Not Whoever This Was.";
            Database.Update(book);

            var bookCheck = Database.Get<Book>(3);
            Assert.Equal("Not Whoever This Was.", bookCheck.Author.Name);
        }
    }
}
