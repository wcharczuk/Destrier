using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Destrier.Test
{
    public enum SubObjectTypeId
    {
        Main = 1,
        Alternate = 2,
        Tertiary = 3
    }

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
        public Int32 CollectionObjetId { get; set; }

        [Column]
        public String SubCollectionObjectName { get; set; }
    }

}
