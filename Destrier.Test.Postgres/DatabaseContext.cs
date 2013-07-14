using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Destrier.Test.Postgres
{
    public class DatabaseContext
    {
        public DatabaseContext()
        {
            SchemaName = "public";
            DatabaseName = "tempdb";
            ConnectionName = "default";
            ConnectionString = "User ID=tempdb_user; Password=passw0rd; Server=localhost; Port=5432; Database=tempdb";
            ProviderName = "Npgsql";
            SetupDatabaseContext();
        }

        public DatabaseContext(String connectionName, String connectionString, String providerName)
        {
            this.ConnectionName = connectionName;
            this.ConnectionString = connectionString;
            this.ProviderName = providerName;
            this.SetupDatabaseContext();
        }
        public String DatabaseName { get; set; }
        public String ConnectionName { get; set; }
        public String ConnectionString { get; set; }
        public String ProviderName { get; set; }
        public String SchemaName { get; set; }

        public void SetupDatabaseContext()
        {
            if (!Destrier.DatabaseConfigurationContext.ConnectionStrings.ContainsKey(ConnectionName))
            {
                if (String.IsNullOrEmpty(ProviderName))
                {
                    Destrier.DatabaseConfigurationContext.ConnectionStrings.Add(ConnectionName, ConnectionString);
                }
                else
                {
                    Destrier.DatabaseConfigurationContext.AddConnectionString(ConnectionName, ConnectionString, ProviderName);
                }
            }

            if (!String.IsNullOrEmpty(this.SchemaName))
                DatabaseConfigurationContext.DefaultSchemaName = SchemaName;

            if (!String.IsNullOrEmpty(this.DatabaseName))
                DatabaseConfigurationContext.DefaultDatabaseName = DatabaseName;
        }
    }

    /// <summary>
    /// This context uses MSSQL specific features and is not recommended for PSQL testing.
    /// </summary>
    public class TestObjectContext : DatabaseContext, IDisposable
    {
        public TestObjectContext() : base() 
        {
            EnsureDestroyDataStore();
            EnsureInitDataStore();
        }

        public TestObjectContext(String connectionName, String connectionString, String providerName)
            : base(connectionName, connectionString, providerName)
        {
            EnsureInitDataStore();
        }

        public Boolean TestIfSchemaExists()
        {
            var exists = false;
            return exists;
        }

        public void EnsureInitDataStore()
        {
            Destrier.DatabaseConfigurationContext.DefaultDatabaseName = "tempdb";
            var initDbScript = @"
CREATE TABLE TestObjects
( 
    id serial primary key, 
    name varchar(255) not null,
    nullname varchar(255),
    mockobjecttypeid smallint not null, 
    active boolean not null,
    created timestamp not null,
    modified timestamp,
    nullableid int,
    referencedobjectid int,
    type int,
    nullabletype int,
    singlechar char,
    single float,
    double float,
    nullabledouble float,
    guid uuid not null,
    nullableguid uuid
);

CREATE OR REPLACE FUNCTION GenerateData() RETURNS SETOF TestObjects AS
$BODY$
DECLARE 
	id int;
	i int := 0;
	subId smallint :=1 ;
	typeId smallint := 1 ;
	nullableTypeId smallint;
	nullableGuid uuid;
BEGIN
	WHILE i < 10001
	LOOP
	    INSERT INTO TestObjects 
	    (Name, mockObjectTypeId, active, created, modified, nullableId, referencedObjectId, type, nullableType, singleChar, single, double, nullableDouble, guid, nullableGuid) 
	    VALUES 
	    ( 'name' || cast(i as varchar), @typeId, true, now(), null, null, subId, 1, nullableTypeId, 'c', 1, 1, nullableTypeId, uuid_generate_v1(), nullableGuid);

		IF (nullableTypeId is null) 
		THEN 
			nullableTypeId = 1; 
		ELSIF(nullableTypeId is not null) THEN 
			nullableTypeId = null; 
		END IF;
	    
		IF (nullableGuid is null) THEN 
			nullableGuid = uuid_generate_v1(); 	
		ELSIF(nullableGuid is not null) 
		THEN 
			nullableGuid = null; 
		END IF;

	    IF (subId = 100) THEN subId = 1; END IF;
	    IF (typeId = 10) THEN typeId = 1; END IF;

	    subId = subId + 1;
	    typeId = typeId + 1;
	    i = i + 1;
	END LOOP;
END;
$BODY$
LANGUAGE 'plpgsql';

SELECT GenerateData();
";

            var initProcScript = @"
CREATE PROCEDURE GetTestObjects_prc 
(
    @limit int = null
)
AS 
BEGIN;
    IF (@limit is null) BEGIN;
        SELECT Id, Name, NullName, Active, Created, Modified, NullableId, ReferencedObjectId, Type, NullableType, SingleChar, [Single], [Double], [NullableDouble], [Guid], [NullableGuid] from TestObjects (nolock);
    END;
    ELSE BEGIN;
        SELECT TOP (@limit) Id, Name, NullName, Active, Created, Modified, NullableId, ReferencedObjectId, Type, NullableType, SingleChar, [Single], [Double], [NullableDouble], [Guid], [NullableGuid] from TestObjects (nolock);
    END;
END";

            if (!TestIfSchemaExists())
            {
                Destrier.Execute.NonQuery(initDbScript);
                //Destrier.Execute.NonQuery(initProcScript);
            }
        }

        public void EnsureDestroyDataStore()
        {
            var statement = @"DROP TABLE IF EXISTS tempdb.public.TestObjects CASCADE;";
            Execute.NonQuery(statement);
        }

        public void Dispose()
        {
            EnsureDestroyDataStore();
        }
    }

    public class LibraryContext : DatabaseContext, IDisposable
    {
        public LibraryContext()
            : base()
        {
            EnsureDestroyDataStore();
            EnsureInitDataStore();
        }

        public LibraryContext(String connectionName, String connectionString, String providerName)
            : base(connectionName, connectionString, providerName)
        {
            EnsureInitDataStore();
        }

        public Boolean TestIfSchemaExists()
        {
            var exists = false;
            return exists;
        }

        public void EnsureInitDataStore()
        {
            Destrier.DatabaseConfigurationContext.DefaultDatabaseName = "tempdb";
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

            if (!TestIfSchemaExists())
            {
                Destrier.Execute.NonQuery(initDbScript);
            }
        }

        public void EnsureDestroyDataStore()
        {
            var statement = @"
DROP TABLE IF EXISTS tempdb.public.Pages;
DROP TABLE IF EXISTS tempdb.public.Chapters;
DROP TABLE IF EXISTS tempdb.public.Books;
DROP TABLE IF EXISTS tempdb.public.People;
";
            Execute.NonQuery(statement);
        }

        public void Dispose()
        {
            EnsureDestroyDataStore();
        }
    }
}
