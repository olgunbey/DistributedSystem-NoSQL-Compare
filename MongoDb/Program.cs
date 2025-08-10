
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text.Json;

static async Task AddToCollection()
{

    var connectionString = "mongodb://127.0.0.1:27017,127.0.0.1:27018,127.0.0.1:27019/,127.0.0.1:27020/?replicaSet=rs0";

    using (MongoClient client = new MongoClient(connectionString))
    {
        var database = client.GetDatabase("test");

        var person = new Person
        {
            Name = "Olgunbey Şahin",
            Age = 24
        };
        var options = new MongoCollectionSettings
        {
            WriteConcern = new WriteConcern(WriteConcern.WMode.Majority)
        };
        var collection = database.GetCollection<BsonDocument>("testCollection", options);
        var bsonDocument = BsonDocument.Parse(JsonSerializer.Serialize(person));
        await collection.InsertOneAsync(bsonDocument);
    }
}

static async Task<List<BsonDocument>> ReadCollection()
{
    var connectionString = "mongodb://127.0.0.1:27017,127.0.0.1:27018,127.0.0.1:27019/,127.0.0.1:27020/?replicaSet=rs0";

    using (MongoClient client = new MongoClient(connectionString))
    {
        var database = client.GetDatabase("test");

        var person = new Person
        {
            Name = "John Doe",
            Age = 30
        };
        var options = new MongoCollectionSettings
        {
            ReadConcern = new ReadConcern(ReadConcernLevel.Available)
        };
        try
        {
            var collection = database.GetCollection<BsonDocument>("testCollection", options);
            var bsonDocument = BsonDocument.Parse(JsonSerializer.Serialize(person));
            var data =(await collection.FindAsync(FilterDefinition<BsonDocument>.Empty)).ToList();
            return data;
        }
        catch (Exception)
        {

            throw;
        }
       

        
    }
}

Console.WriteLine("Hello, World!");

await AddToCollection();
//await ReadCollection();

Console.ReadLine();



public class Person
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}

