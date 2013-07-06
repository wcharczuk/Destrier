using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Destrier.Test
{
    #region Constants

    public enum SubObjectTypeId
    {
        Main = 1,
        Alternate = 2,
        Tertiary = 3
    }

    public enum TestObjectTypeId
    {
        One = 1,
        Two = 2,
        Three = 3
    }

    #endregion

    #region Data Type Tests

    [Table("TestObjects")]
    public class TestObject
    {
        [Destrier.Column(IsPrimaryKey = true)]
        public Int32 Id { get; set; }

        [Destrier.Column(CanBeNull = false)]
        public Boolean Active { get; set; }

        [Destrier.Column(CanBeNull = false)]
        public String Name { get; set; }

        [Destrier.Column]
        public String NullName { get; set; }

        [Destrier.Column(CanBeNull = false)]
        public DateTime Created { get; set; }

        [Destrier.Column]
        public DateTime? Modified { get; set; }

        [Destrier.Column(CanBeNull = false)]
        public Int32 ReferencedObjectId { get; set; }

        [Destrier.Column]
        public Int32? NullableId { get; set; }

        [Destrier.Column(CanBeNull = false)]
        public TestObjectTypeId Type { get; set; }

        [Destrier.Column]
        public TestObjectTypeId? NullableType { get; set; }

        [Destrier.Column(CanBeNull = false)]
        public String SingleChar { get; set; }

        [Destrier.Column(CanBeNull = false)]
        public Double Single { get; set; }

        [Destrier.Column(CanBeNull = false)]
        public Double Double { get; set; }

        [Destrier.Column]
        public Double? NullableDouble { get; set; }

        [Destrier.Column(CanBeNull = false)]
        public Guid Guid { get; set; }

        [Destrier.Column]
        public Guid? NullableGuid { get; set; }
    }

    #endregion

    #region Reflection Tests
    [Table(TableName = "MockObjects")]
    public class MockObject
    {
        [Column(IsPrimaryKey = true)]
        public Int32 MockObjectId { get; set; }

        [Column]
        public Boolean Active { get; set; }

        [Column]
        public String MockObjectName { get; set; }

        [Column]
        public DateTime Created { get; set; }

        [Column]
        public DateTime? Modified { get; set; }

        [Column]
        public Int32 SubObjectId { get; set; }

        [Column]
        public Int32? NullableId { get; set; }

        [ReferencedObject("SubObjectId")]
        public SubObject ReferencedSubObject { get; set; }

        [Column]
        public Int32 AnotherSubObjectId { get; set; }

        [ReferencedObject("AnotherSubObjectId")]
        public SubObject AnotherReferencedSubObject { get; set; }

        [Column]
        public Int32 ReferenceLoopObjectId { get; set; }

        [ReferencedObject("ReferenceLoopObjectId")]
        public ReferenceLoopObject ChildObject { get; set; }

        [ChildCollection]
        public List<CollectionObject> CollectionObjects { get; set; }

        [ChildCollection(AlwaysInclude = true)]
        public List<AlwaysIncludeCollectionObject> AlwaysIncludeCollectionObjects { get; set; }
    }

    [Table(TableName = "SubObjects")]
    public class SubObject
    {
        [Column(IsPrimaryKey = true)]
        public Int32 SubObjectId { get; set; }

        [Column]
        public SubObjectTypeId SubObjectTypeId { get; set; }

        [Column]
        public String SubObjectName { get; set; }
    }

    [Table(TableName = "ReferenceLoopObjects")]
    public class ReferenceLoopObject
    {
        [Column(IsPrimaryKey = true)]
        public Int32 ReferenceLoopObjectId { get; set; }

        [Column]
        public String ReferenceLoopObjectName { get; set; }

        [Column]
        public Int32 MockObjectId { get; set; }

        //refernce loop. AHHH
        [ReferencedObject("MockObjectId")]
        public MockObject MockObject { get; set; }
    }

    [Table("AlwaysIncludeCollectionObjects")]
    public class AlwaysIncludeCollectionObject
    {
        [Column(IsPrimaryKey=true)]
        public Int32 AlwaysIncludeCollectionObjectId { get; set; }

        [Column]
        public Int32 MockObjectId { get; set; }

        [Column]
        public String AlwaysIncludeCollectionObjectName { get; set; }
    }

    [Table(TableName = "CollectionObjects")]
    public class CollectionObject
    {
        [Column(IsPrimaryKey = true)]
        public Int32 CollectionObjectId { get; set; }

        [Column]
        public Int32 MockObjectId { get; set; }

        [Column]
        public String CollectionObjectName { get; set;  }

        [ChildCollection]
        public List<SubCollectionObject> SubCollectionObjects { get; set; }
    }

    [Table(TableName = "SubCollectionObjects")]
    public class SubCollectionObject
    {
        [Column(IsPrimaryKey = true)]
        public Int32 SubCollectionObjectId { get; set;}

        [Column]
        public Int32 CollectionObjectId { get; set; }

        [Column]
        public String SubCollectionObjectName { get; set; }
    }

    #endregion

    #region Modeling Tests

    [Table("Books")]
    public class Book : IPreRemove, IPreCreate, IPostUpdate
    {
        [Column(IsPrimaryKey=true, IsAutoIdentity=true)]
        public int Id { get; set; }

        [Column(CanBeNull=false)]
        public string Title { get; set; }

        [Column(CanBeNull=false)]
        public short Year { get; set; }

        [Column(CanBeNull = false)]
        public int AuthorId { get; set; }

        [ReferencedObject("AuthorId")]
        public Person Author { get; set; }

        [Column]
        public String Notes { get; set; }

        [ChildCollection("BookId", AlwaysInclude = true)]
        public List<Chapter> Chapters { get; set; }

        public void PreRemove()
        {
            Database.RemoveWhere<Page>(p => p.BookId == this.Id);
            Database.RemoveWhere<Chapter>(c => c.BookId == this.Id);
        }

        public void PreCreate()
        {
            if (Author.Id == 0)
                Database.Create(Author);

            this.AuthorId = this.Author.Id;
        }

        public void PostUpdate()
        {
            Database.Update(Author);
        }
    }

    [Table("People")]
    public class Person 
    {
        [Column(IsPrimaryKey = true, IsAutoIdentity = true)]
        public int Id { get; set; }

        [Column(CanBeNull=false)]
        public string Name { get; set; }
    }

    [Table("Chapters")]
    public class Chapter
    {
        [Column(IsPrimaryKey = true, IsAutoIdentity = true)]
        public int Id { get; set; }

        [Column(CanBeNull = false)]
        public int Number { get; set; }

        [Column(CanBeNull = false)]
        public string Title { get; set; }

        [Column(CanBeNull = false)]
        public int BookId { get; set; }

        [ChildCollection("ChapterId")]
        public List<Page> Pages { get; set; }
    }

    [Table("Pages")]
    public class Page
    {
        [Column(IsPrimaryKey = true, IsAutoIdentity = true)]
        public int Id { get; set; }

        [Column(CanBeNull = false)]
        public int Number { get; set; }

        [Column(CanBeNull = false)]
        public int BookId { get; set; }

        [Column(CanBeNull = false)]
        public int ChapterId { get; set; }

        [Column(CanBeNull = false, MaxStringLength = 1024, ShouldTrimLongStrings = false)]
        public String Text { get; set; }

        [ReferencedObject("BookId")]
        public Book Book { get; set; }

        [ReferencedObject("ChapterId")]
        public Chapter Chapter { get; set; }
    }

    #endregion
}
