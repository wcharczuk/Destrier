#Destrier#
Destrier is a flexible yet minimal ORM for .net.

It is designed to leverage both strong typing and model / schema relationships and push the developer towards
using stored procedures for complicated queries (read: anything with 'group by' or 'join').

###Features###
* POCO support; use your existing objects.
* Code first based on annotations.
* Speed: It's pretty fast for what it lets you do.
* Expressive: Strongly typed query syntax and update syntax.
* Better update handling: use the Update class to specify individual sets and a where constraint. Only touch what data you absoultely need to.
* "Referenced Objects" let you have associated objects (joined to specified properties).
 * Say an object has a 'UserId' property; Destrier will automatically fill a 'User' object based on the specified reference.
* "Child Objects" let you have related sub collections (one-to-many relationships).
* IEnumerable reader let you stream results from large datasets / queries.

###Speed###
The following test was performed on 100 iterations for each orm, selecting an object from a table limiting to 2000 results.

| ORM                  | Timing        |
|----------------------|---------------|
|Raw Reader            | Avg:	  3.49ms | 
|Dapper                | Avg:	  4.03ms | 
|ServiceStack ORMLite  | Avg:   7.45ms |
|Destrier              | Avg:   8.16ms |
|EntityFramework       | Avg:  48.73ms |

###Core Components###
* DatabaseConfigurationContext: Where you set your connection strings.
* Table Attribute: Tells the orm what table to map the class to
* Column Attribute: Tells the orm what properties to map to columns in the schema.
* IPopulate: Use this interface to tell the ORM how to populate your objects from data readers (if you don't want to mark columns with the Column attribute).
* Query<T>: The main construct for querying.
  * Where(predicate): Add a where constraint by lambda
  * Include(collection_prop): Tells the ORM to include a child collection.
  * OrderBy(prop): Order the results by the specified member.
  * Limit(int): Limit the results to N rows.
  * Execute(): Run the query, return a list of <T>
* Database<T>: Main functions for simple CRUD operations.
  * Get(id): Get an object by id.
  * Remove(obj): Remove an object instance.
  * RemoveWhere(predicate): Remove by a predicate.
  * Create(obj): Create a new row for the object. Will set the ID column if it's marked AutoIncrement.
  * Update(obj): Update an object. Requires the object to have columns marked as primary key, will update the table with all properties of the object passed in.
* Update<T>: For when you want to do an update and not send down the full contents of an object.
  * Set(member, value): Sets the column in the database for the specified member to the value.
  * Where(predicate): The 'Where' constraint on the update (typically, super important).

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

