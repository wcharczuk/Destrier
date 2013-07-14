using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Destrier.Test
{
    public class DatabaseContext
    {
        public DatabaseContext()
        {
            ConnectionName = "default";
            ConnectionString = "Data Source=localhost;Initial Catalog=tempdb;Integrated Security=True";
            SetupDatabaseContext();
        }

        public DatabaseContext(String connectionName, String connectionString, String providerName)
        {
            this.ConnectionName = connectionName;
            this.ConnectionString = connectionString;
            this.ProviderName = providerName;
            this.SetupDatabaseContext();
        }

        public String ConnectionName { get; set; }
        public String ConnectionString { get; set; }
        public String ProviderName { get; set; }

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
        }
    }

    public class TestObjectContext : DatabaseContext, IDisposable
    {
        public TestObjectContext() : base() 
        {
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
            Execute.StatementReader("SELECT OBJECT_ID('tempdb..TestObjects')", (dr) =>
            {
                while (dr.Read())
                {
                    exists = !dr.IsDBNull(0);
                }
            });
            return exists;
        }

        public void EnsureInitDataStore()
        {
            Destrier.DatabaseConfigurationContext.DefaultDatabaseName = "tempdb";
            var initDbScript = @"
CREATE TABLE TestObjects
( 
    id int not null identity(1,1) primary key, 
    name varchar(255) not null,
    nullName varchar(255),
    mockObjectTypeId smallint not null, 
    active bit not null,
    created datetime not null,
    modified datetime,
    nullableId int,
    referencedObjectId int,
    [type] int,
    [nullableType] int,
    [singleChar] char,
    [single] float,
    [double] float,
    [nullableDouble] float,
    [guid] uniqueidentifier not null,
    [nullableGuid] uniqueidentifier
);

CREATE TABLE Ids
(
    id int not null primary key
);

DECLARE @id int;
DECLARE @i int;
DECLARE @subId int;
DECLARE @typeId smallint;
DECLARE @nullableTypeId smallint;
DECLARE @nullableGuid uniqueidentifier;

SET @i = 0;
SET @subId = 1;
SET @typeId = 1;

WHILE @i < 10001
BEGIN
    INSERT INTO TestObjects 
    ([Name], [mockObjectTypeId], [active], [created], [modified], [nullableId], [referencedObjectId], [type], [nullableType], [singleChar], [single], [double], [nullableDouble], [guid], [nullableGuid]) 
    VALUES 
    ( 'name' + cast(@i as varchar), @typeId, 1, getdate(), null, null, @subId, 1, @nullableTypeId, 'c', 1, 1, @nullableTypeId, newid(), @nullableGuid);
    
    INSERT INTO Ids VALUES (@i);

    IF(@nullableTypeId is null) BEGIN; set @nullableTypeId = 1; END;
    ELSE IF(@nullableTypeId is not null) BEGIN; set @nullableTypeId = null; END;
    
    IF(@nullableGuid is null) BEGIN set @nullableGuid = NEWID(); END;
    ELSE IF(@nullableGuid is not null) BEGIN; set @nullableGuid = null; END;

    IF(@subId = 100) BEGIN; SET @subId = 1; END;
    IF(@typeId = 10) BEGIN; SET @typeId = 1; END;

    SET @subId = @subId + 1;
    SET @typeId = @typeId + 1;
    SET @i = @i + 1;
END
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
                Destrier.Execute.NonQuery(initProcScript);
            }
        }

        public void EnsureDestroyDataStore()
        {
            var statement = @"
if (OBJECT_ID('tempdb..TestObjects') is not null)
BEGIN
    DROP TABLE tempdb..TestObjects
END

if (OBJECT_ID('tempdb..Ids') is not null)
BEGIN
    DROP TABLE tempdb..Ids
END

IF(OBJECT_ID('tempdb..GetTestObjects_prc') is not null)
BEGIN
	DROP PROCEDURE GetTestObjects_prc;
END
";
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
            Execute.StatementReader("SELECT OBJECT_ID('tempdb..Books')", (dr) =>
            {
                while (dr.Read())
                {
                    exists = !dr.IsDBNull(0);
                }
            });
            return exists;
        }

        public void EnsureInitDataStore()
        {
            Destrier.DatabaseConfigurationContext.DefaultDatabaseName = "tempdb";
            var initDbScript = @"
CREATE TABLE People
(
id int not null primary key identity(1,1),
name varchar(255) not null
);

CREATE TABLE Books
(
id int not null primary key identity(1,1),
title varchar(255) not null,
year smallint not null,
authorId int not null foreign key references People(Id),
notes varchar(255)
);

CREATE TABLE Chapters
(
id int not null primary key identity(1,1),
title varchar(255) not null,
number int not null,
bookId int not null
);

CREATE TABLE Pages
(
id int not null primary key identity(1,1),
number int not null,
bookId int not null foreign key references Books(Id),
chapterId int not null foreign key references Chapters(Id),
text nvarchar(1024) not null
);

INSERT INTO People VALUES ('Ernest Hemingway'); --1
INSERT INTO People VALUES ('T. S. Eliot'); --2
INSERT INTO People VALUES ('William Shakespeare'); --3
INSERT INTO People VALUES ('F. Scott Fitzgerald'); --4
INSERT INTO People VALUES ('Henry David Thoreau'); --5

INSERT INTO Books VALUES ('The Old Man and the Sea', 1952, 1, null); --1
INSERT INTO Books VALUES ('The Waste Land', 1922, 2, null); --2
INSERT INTO Books VALUES ('Hamlet', 1603, 3, null); --3
INSERT INTO Books VALUES ('Macbeth', 1611, 3, null); --4
INSERT INTO Books VALUES ('The Great Gatsby', 1925, 4, null); --5
INSERT INTO Books VALUES ('Walden', 1854, 5, null); --6

INSERT INTO Chapters VALUES ('The Old Man and the Sea', 1, 1);
INSERT INTO Chapters VALUES ('The Burial Of The Dead', 1, 2);
INSERT INTO Chapters VALUES ('A Game of Chess', 2, 2);
INSERT INTO Chapters VALUES ('Death By Water', 3, 2);
INSERT INTO Chapters VALUES ('What The Thunder Said', 4, 2);
INSERT INTO Chapters VALUES ('Elsinore. A platform before the castle.', 1, 3);
INSERT INTO Chapters VALUES ('A room of state in the castle.', 2, 3);
INSERT INTO Chapters VALUES ('A room in Polonius house.', 3, 3);
INSERT INTO Chapters VALUES ('The platform.', 4, 3);
INSERT INTO Chapters VALUES ('Another part of the platform.', 5, 3);
INSERT INTO Chapters VALUES ('A desert place.', 1, 4);
INSERT INTO Chapters VALUES ('A camp near Forres.', 2, 4);
INSERT INTO Chapters VALUES ('A heath near Forres.', 3, 4);
INSERT INTO Chapters VALUES ('Forres. The palace.', 4, 4);
INSERT INTO Chapters VALUES ('Inverness. Macbeths castle.', 5, 4);
INSERT INTO Chapters VALUES ('Before Macbeths castle.', 6, 4);
INSERT INTO Chapters VALUES ('Macbeths castle.', 7, 4);
INSERT INTO Chapters VALUES ('Chapter 1', 1, 5);
INSERT INTO Chapters VALUES ('Chapter 2', 2, 5);
INSERT INTO Chapters VALUES ('Chapter 3', 3, 5);
INSERT INTO Chapters VALUES ('Chapter 4', 4, 5);
INSERT INTO Chapters VALUES ('Chapter 5', 5, 5);
INSERT INTO Chapters VALUES ('Chapter 6', 6, 5);
INSERT INTO Chapters VALUES ('Chapter 7', 7, 5);
INSERT INTO Chapters VALUES ('Chapter 8', 8, 5);
INSERT INTO Chapters VALUES ('Chapter 9', 9, 5);
INSERT INTO Chapters VALUES ('Economy', 1, 6);
INSERT INTO Chapters VALUES ('Where I Lived. & What I Lived for', 2, 6);
INSERT INTO Chapters VALUES ('Reading', 3, 6);
INSERT INTO Chapters VALUES ('Sounds', 4, 6);
INSERT INTO Chapters VALUES ('Solitude', 5, 6);
INSERT INTO Chapters VALUES ('Visitors', 6, 6);
INSERT INTO Chapters VALUES ('The Bean-Field', 7, 6);
INSERT INTO Chapters VALUES ('The Village', 8, 6);
INSERT INTO Chapters VALUES ('The Ponds', 9, 6);
INSERT INTO Chapters VALUES ('Baker Farm', 10, 6);
INSERT INTO Chapters VALUES ('Higer Laws', 11, 6);
INSERT INTO Chapters VALUES ('Brute Neighbors', 12, 6);
INSERT INTO Chapters VALUES ('House-Warming', 13, 6);
INSERT INTO Chapters VALUES ('Former Inhabitants: & Winter Visitors', 14, 6);
INSERT INTO Chapters VALUES ('Winter Animals', 15, 6);
INSERT INTO Chapters VALUES ('The Pond in Winter', 16, 6);
INSERT INTO Chapters VALUES ('Spring', 17, 6);
INSERT INTO Chapters VALUES ('Conclusion', 18, 6);

INSERT INTO Pages VALUES (1, 1, 1, 'He was an old man who fished alone in a skiff in the Gulf Stream and he had gone eighty-four days now without taking a fish.');
INSERT INTO Pages VALUES (2, 1, 1, 'Five and you nearly were killed when I brought the fish in too green and he nearly tore the boat to pieces. Can you remember?');
INSERT INTO Pages VALUES (3, 1, 1, 'They picked up the gear from the boat. The old man carried the mast on his shoulder and the boy carried the wooden boat with the coiled, hard-braided brown lines, the gaff and the harpoon with its shaft.');
";

            if (!TestIfSchemaExists())
            {
                Destrier.Execute.NonQuery(initDbScript);
            }
        }

        public void EnsureDestroyDataStore()
        {
            var statement = @"
if (OBJECT_ID('tempdb..Pages') is not null)
BEGIN
    DROP TABLE tempdb..Pages
END

if (OBJECT_ID('tempdb..Chapters') is not null)
BEGIN
    DROP TABLE tempdb..Chapters
END

if (OBJECT_ID('tempdb..Books') is not null)
BEGIN
    DROP TABLE tempdb..Books
END

if (OBJECT_ID('tempdb..People') is not null)
BEGIN
    DROP TABLE tempdb..People
END
";
            Execute.NonQuery(statement);
        }

        public void Dispose()
        {
            EnsureDestroyDataStore();
        }
    }
}
