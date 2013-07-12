﻿using System;
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
            var commandBuilder = new CommandBuilder<MockObject>(command);
            var commandText = commandBuilder.GenerateSelect();

            Assert.NotNull(commandText);
            Assert.NotEmpty(commandText);
        }

        [Fact]
        public void Select_Where_Test()
        {
            var command = new StringBuilder();
            var commandBuilder = new CommandBuilder<MockObject>(command);
            commandBuilder.AddWhere(mo => mo.MockObjectId == 1);
            var commandText = commandBuilder.GenerateSelect();

            Assert.NotNull(commandText);
            Assert.NotEmpty(commandText);
        }

        [Fact]
        public void Select_Where_Legacy_Test()
        {
            var command = new StringBuilder();
            var commandBuilder = new CommandBuilder<MockObject>(command);
            commandBuilder.AddWhereDynamic(new { MockObjectId = 1 });
            var commandText = commandBuilder.GenerateSelect();

            Assert.NotNull(commandText);
            Assert.NotEmpty(commandText);
        }

        [Fact]
        public void Select_Where_ReferencedObject_Test()
        {
            var command = new StringBuilder();
            var commandBuilder = new CommandBuilder<MockObject>(command);
            commandBuilder.AddWhere( m => m.AnotherReferencedSubObject.SubObjectName == "Test String.");
            var commandText = commandBuilder.GenerateSelect();

            Assert.NotNull(commandText);
            Assert.NotEmpty(commandText);
        }

        [Fact]
        public void Select_Include_Test()
        {
            var commandBuilder = new CommandBuilder<MockObject>();
            commandBuilder.AddIncludedChildCollection(m => m.CollectionObjects);

            var commandText = commandBuilder.GenerateSelect();

            Assert.NotNull(commandText);
            Assert.NotEmpty(commandText);
            Assert.True(commandBuilder.ChildCollections.Count() > 0);
        }

        [Fact]
        public void Select_IncludeByString_Test()
        {
            var commandBuilder = new CommandBuilder<MockObject>();
            commandBuilder.AddIncludedChildCollection("CollectionObjects");
            commandBuilder.AddIncludedChildCollection("CollectionObjects.SubCollectionObjects");

            var commandText = commandBuilder.GenerateSelect();

            Assert.NotNull(commandText);
            Assert.NotEmpty(commandText);
            Assert.True(commandBuilder.ChildCollections.Count() == 3);
        }

        [Fact]
        public void CommandBuilder_Cache_Test()
        {
            var commandBuilder = new CommandBuilder<MockObject>();

            var visitor = new SqlExpressionVisitor<MockObject>();
            Expression<Func<MockObject, bool>> exp = (u) => u.MockObjectId == 1;
            visitor.Visit(exp);
            var sqlText = visitor.Buffer.ToString();
            Assert.Equal(sqlText, String.Format("[MockObjectId] = @{0}", visitor.Parameters.First().Key));
        }

        [Fact]
        public void Update_Test()
        {
            var cb = new CommandBuilder<MockObject>();
            cb.AddSet(m => m.Active, true);
            cb.AddWhere(m => m.MockObjectId == 10);

            var sqlText = cb.GenerateUpdate();

            Assert.NotNull(sqlText);
        }

        [Fact]
        public void Offset_Test()
        {
            var cb = new CommandBuilder<MockObject>();
            cb.Offset = 10;
            cb.Limit = 100;
            var sqlText = cb.GenerateSelect();
            Assert.NotNull(sqlText);
        }

        [Fact]
        public void NoLock_Test()
        {
            var cb = new CommandBuilder<Book>();
            var sqlText = cb.GenerateSelect();
            Assert.True(sqlText.Contains("(NOLOCK)"));

            var cb2 = new CommandBuilder<Ids>();
            var sqlText2 = cb2.GenerateSelect();
            Assert.False(sqlText2.Contains("(NOLOCK)"));
        }
    }
}
