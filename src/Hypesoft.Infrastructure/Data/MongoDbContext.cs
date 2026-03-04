using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Hypesoft.Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string name) =>
        _database.GetCollection<T>(name);

    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        var result = await _database.RunCommandAsync<MongoDB.Bson.BsonDocument>(
            new MongoDB.Bson.BsonDocument("ping", 1), cancellationToken: ct);
        return result != null;
    }
}
