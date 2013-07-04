using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Destrier.Test
{
    public class DatabaseTest
    {
        private static Boolean _didInitDataStore = false;
        public DatabaseTest()
        {
            SetupDatabaseContext();

            if (!_didInitDataStore)
            {
                EnsureInitDataStore();
                _didInitDataStore = true;
            }
        }

        public const String ConnectionString = "Data Source=localhost;Initial Catalog=tempdb;Integrated Security=True";
        public void SetupDatabaseContext()
        {
            Destrier.DatabaseConfigurationContext.ConnectionStrings.Add("default", ConnectionString);
            Destrier.DatabaseConfigurationContext.DefaultConnectionName = "default";
            Destrier.DatabaseConfigurationContext.DefaultDatabaseName = "tempdb";
        }

        public static void EnsureInitDataStore()
        {
            var initDbScript = @"
if (OBJECT_ID('tempdb..TestObjects') is not null)
BEGIN
    DROP TABLE TestObjects
END

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
    [type] smallint,
    [nullableType] smallint
);

DECLARE @id int;
DECLARE @i int;
DECLARE @subId int;
DECLARE @typeId smallint;
DECLARE @nullableTypeId smallint;

SET @i = 0;
SET @subId = 1;
SET @typeId = 1;

WHILE @i < 5001
BEGIN
    INSERT INTO TestObjects ([Name], [mockObjectTypeId], [active], [created], [modified], [nullableId], [referencedObjectId], [type], [nullableType]) VALUES ( 'name' + cast(@i as varchar), @typeId, 1, getdate(), null, null, @subId, 1, @nullableTypeId);
    
    IF(@nullableTypeId is null) BEGIN; set @nullableTypeId = 1; END;
    IF(@nullableTypeId is not null) BEGIN; set @nullableTypeId = null; END;
    IF(@subId = 100) BEGIN; SET @subId = 1; END;
    IF(@typeId = 10) BEGIN; SET @typeId = 1; END;

    SET @subId = @subId + 1;
    SET @typeId = @typeId + 1;
    SET @i = @i + 1;
END

";
            Destrier.Execute.NonQuery(initDbScript);
        }
    }
}
