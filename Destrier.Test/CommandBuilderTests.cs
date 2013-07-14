using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Destrier.Test
{
    public class CommandBuilderTests
    {
        public CommandBuilderTests()
        {
            Destrier.DatabaseConfigurationContext.DefaultDatabaseName = "DestrierTest";
            Destrier.DatabaseConfigurationContext.DefaultSchemaName = "dbo";
        }

        [Fact]
        public void Select_Test()
        {
            var command = new StringBuilder();
            var commandBuilder = new SqlServerCommandBuilder<MockObject>();
            commandBuilder.Command = command;
            var commandText = commandBuilder.GenerateSelect();

            Assert.NotNull(commandText);
            Assert.NotEmpty(commandText);
        }

        [Fact]
        public void Select_Where_Test()
        {
            var command = new StringBuilder();
            var commandBuilder = new SqlServerCommandBuilder<MockObject>();
            commandBuilder.Command = command;
            commandBuilder.AddWhere(mo => mo.MockObjectId == 1);
            var commandText = commandBuilder.GenerateSelect();

            Assert.NotNull(commandText);
            Assert.NotEmpty(commandText);
        }

        [Fact]
        public void Select_Where_Legacy_Test()
        {
            var command = new StringBuilder();
            var commandBuilder = new SqlServerCommandBuilder<MockObject>();
            commandBuilder.Command = command;
            commandBuilder.AddWhereDynamic(new { MockObjectId = 1 });
            var commandText = commandBuilder.GenerateSelect();

            Assert.NotNull(commandText);
            Assert.NotEmpty(commandText);
        }

        [Fact]
        public void Select_Where_ReferencedObject_Test()
        {
            var command = new StringBuilder();
            var commandBuilder = new SqlServerCommandBuilder<MockObject>();
            commandBuilder.Command = command;
            commandBuilder.AddWhere( m => m.AnotherReferencedSubObject.SubObjectName == "Test String.");
            var commandText = commandBuilder.GenerateSelect();

            Assert.NotNull(commandText);
            Assert.NotEmpty(commandText);
        }

        [Fact]
        public void Select_Include_Test()
        {
            var commandBuilder = new SqlServerCommandBuilder<MockObject>();
            commandBuilder.AddIncludedChildCollection(m => m.CollectionObjects);

            var commandText = commandBuilder.GenerateSelect();

            Assert.NotNull(commandText);
            Assert.NotEmpty(commandText);
            Assert.True(commandBuilder.ChildCollections.Count() > 0);
        }

        [Fact]
        public void Select_IncludeByString_Test()
        {
            var commandBuilder = new SqlServerCommandBuilder<MockObject>();
            commandBuilder.AddIncludedChildCollection("CollectionObjects");
            commandBuilder.AddIncludedChildCollection("CollectionObjects.SubCollectionObjects");

            var commandText = commandBuilder.GenerateSelect();

            Assert.NotNull(commandText);
            Assert.NotEmpty(commandText);
            Assert.True(commandBuilder.ChildCollections.Count() == 3);
        }

        [Fact]
        public void Update_Test()
        {
            var cb = new SqlServerCommandBuilder<MockObject>();
            cb.AddSet(m => m.Active, true);
            cb.AddWhere(m => m.MockObjectId == 10);

            var sqlText = cb.GenerateUpdate();

            Assert.NotNull(sqlText);
        }

        [Fact]
        public void Offset_Test()
        {
            var cb = new SqlServerCommandBuilder<MockObject>();
            cb.Offset = 10;
            cb.Limit = 100;
            var sqlText = cb.GenerateSelect();
            Assert.NotNull(sqlText);
        }
    }
}
