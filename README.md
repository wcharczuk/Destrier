#Destrier#
Destrier is a flexible yet minimal ORM for .net.

It is designed to leverage both strong typing and model / schema relationships and push the developer towards
using stored procedures for complicated queries (read: anything with 'group by' or 'join').

###Core Components###
* DatabaseConfigurationContext: Where you set your connection strings.
* BaseModel: Inherit from this to get more complicated relational functionality like child members and associated objects and query support.
* IPopulate: Use this interface to tell the ORM how to populate your objects from data readers.
* Table Attribute: Tells the orm what table to map the class to
* Column Attribute: Tells the orm what properties to map to columns in the schema.
* Query<T>: The main construct for querying.
  * Where(): Add a where constraint by lambda
  * Include(): Tells the ORM to include a child collection.
  * OrderBy(): Order the results by the specified member.
  * Limit(): Limit the results to N rows.
  * Execute(): Run the query, return a list of <T>
* Database<T>: Main functions for simple CRUD operations.
  * Get(): Get an object by Id
  * Remove(): Remove an instance
  * RemoveWhere(): Remove by a predicate
  * Create(): Create a new row for the object. Will set the ID column if it's marked AutoIncrement.
  * Update(): Update an object. Requires the object to have columns marked as primary key.

###Examples###

First, we set up the database context:
```C#
DatabaseConfigurationContext.ConnectionStrings.Add("default", String.Format("Server={0};Database={1};Trusted_Connection=true", "localhost", "mockDatabase"));
DatabaseConfigurationContext.DefaultConnectionName = "default";
```
Then, given a model like the following:
```C#
//assumes we have a table in database 'mockDatabase' that is called 'mockobjects'
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
```
You can then do the following:
```C#
//create a new object
var mockObj = new MockObject() { MockObjectId = 1, Created = DateTime.Now };
Database.Create(mockObj);

//or query out some existing objects
var results = new Query<MockObject>().Where(mo => mo.Created > DateTime.Now.AddDays(-30)).OrderBy(mo => mo.Created).Limit(5).Execute();
```
To run the following in a stored proc:
```C#
var results = new List<MockObject>();
Execute.StoredProcedureReader("MyStoredProc_prc", (dr) => {
  results = dr.ReadList<MockObject>();
}, { Before = DateTime.Now.AddDays(-30) });
```
You can also use lists in predicates, and the Expression Visitor will translate them into SQL "in" statements.
```C#
var list = new List<Int32>() { 1, 2, 3, 4 };
var results = new Query<MockObject>().Where(mo => list.Contains(mo.MockObjectId)).Execute();

//resulting sql is "WHERE [MockObjectId] in (1,2,3,4)"
```

###Pull Requests / Contributions###
Keep them coming.

