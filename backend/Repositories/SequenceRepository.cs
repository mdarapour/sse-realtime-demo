using MongoDB.Driver;
using SseDemo.Outbox.Models;

namespace SseDemo.Repositories;

/// <summary>
/// MongoDB implementation of the sequence repository
/// </summary>
public class SequenceRepository : ISequenceRepository
{
    private readonly IMongoCollection<SequenceCounter> _collection;
    private readonly ILogger<SequenceRepository> _logger;

    public SequenceRepository(IMongoClient mongoClient, IConfiguration configuration, ILogger<SequenceRepository> logger)
    {
        _logger = logger;
        
        var databaseName = configuration["MongoDB:DatabaseName"] ?? "sse_outbox";
        var database = mongoClient.GetDatabase(databaseName);
        _collection = database.GetCollection<SequenceCounter>("sequences");
    }

    public async Task<long> GetNextSequenceNumberAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<SequenceCounter>.Filter.Eq(x => x.Id, "event_sequence");
        var update = Builders<SequenceCounter>.Update
            .Inc(x => x.CurrentValue, 1)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        var options = new FindOneAndUpdateOptions<SequenceCounter>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };
        
        var result = await _collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        return result.CurrentValue;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Ensure the sequence counter exists
            var filter = Builders<SequenceCounter>.Filter.Eq(x => x.Id, "event_sequence");
            var exists = await _collection.Find(filter).AnyAsync();
            
            if (!exists)
            {
                await _collection.InsertOneAsync(new SequenceCounter
                {
                    Id = "event_sequence",
                    CurrentValue = 0,
                    UpdatedAt = DateTime.UtcNow
                });
                
                _logger.LogInformation("Initialized sequence counter");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing sequence counter");
        }
    }
}