using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Destrier;

namespace Destrier.Test
{
    public class PropertyTestClass
    {
        public Single Single { get; set; }
        public Int32 AnInt { get; set; }
        public Int64 ALong { get; set; }
        public Int32? NullableInt32 { get; set; }
        public DateTime DateTime { get; set; }
    }

    public class ReflectionTests
    {
        public ReflectionTests()
        {
            DatabaseConfigurationContext.DefaultDatabaseName = "DestrierTest";
            DatabaseConfigurationContext.DefaultSchemaName = "dbo";
        }

        [Fact]
        public void Core_Tests()
        {
            var members = ModelCache.GetColumnMemberLookup(typeof(MockObject));

            var notNullable = members["MockObjectId"];
            var nullable = members["NullableId"];

            Assert.False(ReflectionHelper.IsNullableType(notNullable.Type));
            Assert.True(ReflectionHelper.IsNullableType(nullable.Type));
        }

        [Fact]
        public void TableName_Test()
        {
            var tableAttribute = ModelCache.GetTableAttribute(typeof(MockObject));
            Assert.NotNull(tableAttribute);

            var tableAttributeFromModel = ModelCache.GetTableAttribute(typeof(MockObject));
            Assert.Equal(tableAttributeFromModel, tableAttribute);
            var tableName = Model.TableName(typeof(MockObject));

            var fullyQualifiedTableName = Model.TableNameFullyQualified(typeof(MockObject));

            Assert.NotNull(tableName);
            Assert.NotEmpty(tableName);

            Assert.Equal(tableName, "MockObjects");

            Assert.NotNull(fullyQualifiedTableName);
            Assert.NotEmpty(fullyQualifiedTableName);
            Assert.Equal(fullyQualifiedTableName, "DestrierTest.dbo.MockObjects");

            Assert.True(tableAttribute.UseNoLock);

            var idTableAttribute = ModelCache.GetTableAttribute(typeof(Ids));
            Assert.False(idTableAttribute.UseNoLock);
            Assert.False(Model.UseNoLock(typeof(Ids)));
        }

        [Fact]
        public void Members_Test()
        {
            var members = ModelCache.GenerateMembersRecursive(typeof(MockObject));

            Assert.NotNull(members);
            Assert.NotEmpty(members);
            Assert.True(members.Any(m => m is ColumnMember));
            Assert.True(members.Any(m => m is ReferencedObjectMember));
            Assert.True(members.Any(m => m is ChildCollectionMember));
        }
        
        [Fact]
        public void SetPropertyValue_Test()
        {
            var properties = typeof(PropertyTestClass).GetProperties();
            
            var setFunctions = new Dictionary<string, Action<object, object>>();
            var propertiesByName = new Dictionary<string, PropertyInfo>();
            
            foreach (var prop in properties)
            {
                setFunctions.Add(prop.Name, ReflectionHelper.GenerateSetAction(prop));
                propertiesByName.Add(prop.Name, prop);
            }            

            var myObj = new PropertyTestClass();
            myObj.AnInt = 2;

            setFunctions["Single"](myObj, ReflectionHelper.ChangeType(2.0d, propertiesByName["Single"].PropertyType));
        }
    }
}
