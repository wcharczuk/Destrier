using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Destrier;
using Destrier.Test;

namespace PostgresTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            DatabaseConfigurationContext.AddConnectionString("default", "User ID=tempdb_user; Password=passw0rd; Server=localhost; Port=5432; Database=tempdb", "Npgsql");
            DatabaseConfigurationContext.DefaultSchemaName = "public";
            DatabaseConfigurationContext.DefaultDatabaseName = "tempdb";

            Console.Write("Initializing Database ... ");
            InitDb();
            Console.WriteLine("Complete!");

            var books = new Query<Book>().Execute();

            Console.ReadKey();
        }

        static void InitDb()
        {
            var initDbScript = @"
CREATE TABLE People
(
id serial primary key,
name varchar(255) not null
);

CREATE TABLE Books
(
id serial primary key,
title varchar(255) not null,
year smallint not null,
authorId int not null references People(Id),
notes varchar(255)
);

CREATE TABLE Chapters
(
id serial primary key,
title varchar(255) not null,
number int not null,
bookId int not null
);

CREATE TABLE Pages
(
id serial primary key,
number int not null,
bookId int not null references Books(Id),
chapterId int not null references Chapters(Id),
text varchar(1024) not null
);

INSERT INTO People (name) VALUES ('Ernest Hemingway'); --1
INSERT INTO People (name) VALUES ('T. S. Eliot'); --2
INSERT INTO People (name) VALUES ('William Shakespeare'); --3
INSERT INTO People (name) VALUES ('F. Scott Fitzgerald'); --4
INSERT INTO People (name) VALUES ('Henry David Thoreau'); --5

INSERT INTO Books (title, year, authorId, notes) VALUES ('The Old Man and the Sea', 1952, 1, null); --1
INSERT INTO Books (title, year, authorId, notes) VALUES ('The Waste Land', 1922, 2, null); --2
INSERT INTO Books (title, year, authorId, notes) VALUES ('Hamlet', 1603, 3, null); --3
INSERT INTO Books (title, year, authorId, notes) VALUES ('Macbeth', 1611, 3, null); --4
INSERT INTO Books (title, year, authorId, notes) VALUES ('The Great Gatsby', 1925, 4, null); --5
INSERT INTO Books (title, year, authorId, notes) VALUES ('Walden', 1854, 5, null); --6

INSERT INTO Chapters (title, number, bookid) VALUES ('The Old Man and the Sea', 1, 1);
INSERT INTO Chapters (title, number, bookid) VALUES ('The Burial Of The Dead', 1, 2);
INSERT INTO Chapters (title, number, bookid) VALUES ('A Game of Chess', 2, 2);
INSERT INTO Chapters (title, number, bookid) VALUES ('Death By Water', 3, 2);
INSERT INTO Chapters (title, number, bookid) VALUES ('What The Thunder Said', 4, 2);
INSERT INTO Chapters (title, number, bookid) VALUES ('Elsinore. A platform before the castle.', 1, 3);
INSERT INTO Chapters (title, number, bookid) VALUES ('A room of state in the castle.', 2, 3);
INSERT INTO Chapters (title, number, bookid) VALUES ('A room in Polonius house.', 3, 3);
INSERT INTO Chapters (title, number, bookid) VALUES ('The platform.', 4, 3);
INSERT INTO Chapters (title, number, bookid) VALUES ('Another part of the platform.', 5, 3);
INSERT INTO Chapters (title, number, bookid) VALUES ('A desert place.', 1, 4);
INSERT INTO Chapters (title, number, bookid) VALUES ('A camp near Forres.', 2, 4);
INSERT INTO Chapters (title, number, bookid) VALUES ('A heath near Forres.', 3, 4);
INSERT INTO Chapters (title, number, bookid) VALUES ('Forres. The palace.', 4, 4);
INSERT INTO Chapters (title, number, bookid) VALUES ('Inverness. Macbeths castle.', 5, 4);
INSERT INTO Chapters (title, number, bookid) VALUES ('Before Macbeths castle.', 6, 4);
INSERT INTO Chapters (title, number, bookid) VALUES ('Macbeths castle.', 7, 4);
INSERT INTO Chapters (title, number, bookid) VALUES ('Chapter 1', 1, 5);
INSERT INTO Chapters (title, number, bookid) VALUES ('Chapter 2', 2, 5);
INSERT INTO Chapters (title, number, bookid) VALUES ('Chapter 3', 3, 5);
INSERT INTO Chapters (title, number, bookid) VALUES ('Chapter 4', 4, 5);
INSERT INTO Chapters (title, number, bookid) VALUES ('Chapter 5', 5, 5);
INSERT INTO Chapters (title, number, bookid) VALUES ('Chapter 6', 6, 5);
INSERT INTO Chapters (title, number, bookid) VALUES ('Chapter 7', 7, 5);
INSERT INTO Chapters (title, number, bookid) VALUES ('Chapter 8', 8, 5);
INSERT INTO Chapters (title, number, bookid) VALUES ('Chapter 9', 9, 5);
INSERT INTO Chapters (title, number, bookid) VALUES ('Economy', 1, 6);
INSERT INTO Chapters (title, number, bookid) VALUES ('Where I Lived. & What I Lived for', 2, 6);
INSERT INTO Chapters (title, number, bookid) VALUES ('Reading', 3, 6);
INSERT INTO Chapters (title, number, bookid) VALUES ('Sounds', 4, 6);
INSERT INTO Chapters (title, number, bookid) VALUES ('Solitude', 5, 6);
INSERT INTO Chapters (title, number, bookid) VALUES ('Visitors', 6, 6);
INSERT INTO Chapters (title, number, bookid) VALUES ('The Bean-Field', 7, 6);
INSERT INTO Chapters (title, number, bookid) VALUES ('The Village', 8, 6);
INSERT INTO Chapters (title, number, bookid) VALUES ('The Ponds', 9, 6);
INSERT INTO Chapters (title, number, bookid) VALUES ('Baker Farm', 10, 6);
INSERT INTO Chapters (title, number, bookid) VALUES ('Higer Laws', 11, 6);
INSERT INTO Chapters (title, number, bookid) VALUES ('Brute Neighbors', 12, 6);
INSERT INTO Chapters (title, number, bookid) VALUES ('House-Warming', 13, 6);
INSERT INTO Chapters (title, number, bookid) VALUES ('Former Inhabitants: & Winter Visitors', 14, 6);
INSERT INTO Chapters (title, number, bookid) VALUES ('Winter Animals', 15, 6);
INSERT INTO Chapters (title, number, bookid) VALUES ('The Pond in Winter', 16, 6);
INSERT INTO Chapters (title, number, bookid) VALUES ('Spring', 17, 6);
INSERT INTO Chapters (title, number, bookid) VALUES ('Conclusion', 18, 6);

INSERT INTO Pages (number, bookId, chapterId, text) VALUES (1, 1, 1, 'He was an old man who fished alone in a skiff in the Gulf Stream and he had gone eighty-four days now without taking a fish.');
INSERT INTO Pages (number, bookId, chapterId, text) VALUES (2, 1, 1, 'Five and you nearly were killed when I brought the fish in too green and he nearly tore the boat to pieces. Can you remember?');
INSERT INTO Pages (number, bookId, chapterId, text) VALUES (3, 1, 1, 'They picked up the gear from the boat. The old man carried the mast on his shoulder and the boy carried the wooden boat with the coiled, hard-braided brown lines, the gaff and the harpoon with its shaft.');
";
            try
            {
                Destrier.Execute.NonQuery(initDbScript);
            }
            catch { }
        }
    }
}
