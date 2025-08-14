
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text.Json;


#region WriteConcernOneAndReadConcernLinearizableTestCase
WriteConcernOneAndReadConcernLinearizableTestCase testCase1 = new WriteConcernOneAndReadConcernLinearizableTestCase();
await testCase1.AddToCollection();
await testCase1.ReadToCollection();
#endregion



public class Person
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}

public abstract class MongoDbExecuter
{
    public abstract Task AddToCollection();
    public abstract Task<List<BsonDocument>> ReadToCollection();
    public IMongoDatabase GetMongoDatabase()
    {
        var connectionString = "mongodb://127.0.0.1:27017,127.0.0.1:27018,127.0.0.1:27019/,127.0.0.1:27020/?replicaSet=rs0";
        return new MongoClient(connectionString).GetDatabase("test");
    }
}

public class WriteConcernOneAndReadConcernLinearizableTestCase : MongoDbExecuter
{
    private readonly IMongoDatabase _mongoClient;
    public WriteConcernOneAndReadConcernLinearizableTestCase()
    {
        _mongoClient = base.GetMongoDatabase();
    }
    public override Task AddToCollection()
    {
        var person = new Person
        {
            Name = "TestData 123",
            Age = 19
        };
        var options = new MongoCollectionSettings
        {
            WriteConcern = new WriteConcern(new WriteConcern.WCount(1))
        };
        var collection = _mongoClient.GetCollection<BsonDocument>("testCollection", options);
        var bsonDocument = BsonDocument.Parse(JsonSerializer.Serialize(person));
        return collection.InsertOneAsync(bsonDocument);
    }

    public override async Task<List<BsonDocument>> ReadToCollection()
    {
        var options = new MongoCollectionSettings
        {
            ReadConcern = new ReadConcern(ReadConcernLevel.Linearizable)
        };
        var collection = _mongoClient.GetCollection<BsonDocument>("testCollection", options);
        var data = await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();

        Console.WriteLine();
        return data;
    }
}

