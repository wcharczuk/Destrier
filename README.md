#Destrier#

[![Build Status](https://travis-ci.org/ClothesHorse/Destrier.png?branch=master)](https://travis-ci.org/ClothesHorse/Destrier)

Destrier is a flexible yet minimal ORM for .net targeting both MSSQL and Postgres.

[Documentation](https://github.com/ClothesHorse/Destrier/wiki)

It is designed to leverage both strong typing and model / schema relationships and push the developer towards
using stored procedures / functions for complicated queries.

###Features###
* POCO support; use your existing objects.
* Code first based on annotations.
* Type stability: Use Enums and nullable types while mapping db types to your objects.
* Speed: It's pretty fast for what it lets you do.
* Expressive: Strongly typed query syntax and update syntax help catch errors on compilation.
* Database Paging Support: use Offset on queries to enable database paging.
* Better update handling: use the Update class to specify individual sets and a where constraint. Only touch what data you absoultely need to.
* Referenced Objects: let you have associated objects (joined to specified properties).
* Child Collections: let you have related sub collections (one-to-many relationships).
* Lazy<T> support: Both ReferencedObjects and ChildCollections can be bound to properties with Lazy<> types, meaning that they will not affect the initial query (such as add joins to the query etc.) but will be evaluated when they are accessed by .Value.
* IEnumerable reader let you stream results from large datasets / queries.
* Polyglot: Target both SqlServer and Postgres.

###Speed###
The following test was performed on 100 iterations for each orm, selecting an object from a table limiting to 5000 results.

| ORM                  | Timing         |
|----------------------|----------------|
|Raw Reader            | Avg:	16.24ms | 
|PetaPoco              | Avg:   19.00ms | 
|Destrier              | Avg:   19.25ms |
|Destrier (Raw Query)  | Avg:   19.49ms |
|Dapper                | Avg:	20.06ms | 
|EntityFramework 6.1   | Avg:   23.82ms |
|ServiceStack ORMLite  | Avg:   47.86ms |
|EntityFramework 5     | Avg:  112.37ms |

It should be noted that EntityFramework 5 had to have some members disabled because it lacks Enum support. 
Also Entity Framework 6.1 had "AsNoTracking()" enabled on the db calls.
Also should be noted that ORMLite failed to cast Doubles=>Singles. 
Also PetaPoco doesn't handle nullable enums.

Test was run in .net 4.5, Release without debugger attached (ctrl-f5) against MSSQL.

###Core Components###
* DatabaseConfigurationContext: Where you set your connection strings.
* Table Attribute: Tells the orm what table to map the class to
* Column Attribute: Tells the orm what properties to map to columns in the schema.
* IPopulate: Use this interface to tell the ORM how to populate your objects from data readers (if you don't want to mark columns with the Column attribute).
* Query&lt;T&gt;: The main construct for querying.
* Database: Main functions for simple CrUD operations.
* Update&lt;T&gt;: For when you want to do an update and not send down the full contents of an object.

###Examples###

First, we set up the database context:
```C#
DatabaseConfigurationContext.ConnectionStrings.Add("default", "Data Source=.;Initial Catalog=tempdb;Integrated Security=True");
```
Then, given a model like the following:
```C#
//assumes we have a table in database 'mockDatabase' that is called 'mockobjects'
[Table(TableName = "MockObjects")]
public class MockObject
{
    [Column(IsPrimaryKey = true)]
    public Int32 Id { get; set; }

    [Column]
    public Boolean Active { get; set; }

    [Column]
    public String Name { get; set; }

    [Column]
    public DateTime Created { get; set; }

    [Column]
    public DateTime? Modified { get; set; }
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
You can use lists in predicates, and the Expression Visitor will translate them into SQL "in" statements.
```C#
var list = new List<Int32>() { 1, 2, 3, 4 };
var results = new Query<MockObject>().Where(mo => list.Contains(mo.MockObjectId)).Execute();

//resulting sql is "WHERE [MockObjectId] in (1,2,3,4)"
```
You can update objects by individual properties.
```C#
new Update<MockObject>().Set(mo => mo.Active, false).Where(mo => mo.MockObjectId == 2).Exeute();
//resulting sql is UPDATE [alias] SET Active = 0 FROM MockObjects [alias] where MockObjectId = 2
```

Alternately you can update a whole object at once
```C#
var mo = Database.Get<MockObject>(2);
mo.Active = false;
Database.Update(mo);
//this will cause a massive update statement with every property as 'SET's
```

###Documentation###
See the [wiki](https://github.com/ClothesHorse/Destrier/wiki).

###Pull Requests / Contributions###
Keep them coming.

