using MongoDB.Driver;
using SseDemo.Outbox.Models;

namespace SseDemo.Repositories;

/// <summary>
/// MongoDB implementation of the outbox event repository
/// </summary>
public class OutboxEventRepository : IOutboxEventRepository
{
    private readonly IMongoCollection<SseOutboxEvent> _collection;
    private readonly ILogger<OutboxEventRepository> _logger;

    public OutboxEventRepository(IMongoClient mongoClient, IConfiguration configuration, ILogger<OutboxEventRepository> logger)
    {
        _logger = logger;
        
        var databaseName = configuration["MongoDB:DatabaseName"] ?? "sse_outbox";
        var collectionName = configuration["MongoDB:OutboxCollection"] ?? "outbox_events";
        
        var database = mongoClient.GetDatabase(databaseName);
        _collection = database.GetCollection<SseOutboxEvent>(collectionName);
    }

    public async Task PublishEventAsync(SseOutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            await _collection.InsertOneAsync(outboxEvent, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventId} to outbox", outboxEvent.EventId);
            throw;
        }
    }

    public async Task<List<SseOutboxEvent>> GetEventsAfterSequenceAsync(long sequenceNumber, int limit = 100, CancellationToken cancellationToken = default)
    {
        var filter = Builders<SseOutboxEvent>.Filter.Gt(x => x.SequenceNumber, sequenceNumber);
        var sort = Builders<SseOutboxEvent>.Sort.Ascending(x => x.SequenceNumber);
        
        return await _collection
            .Find(filter)
            .Sort(sort)
            .Limit(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<SseOutboxEvent?> GetLatestEventAsync(CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(FilterDefinition<SseOutboxEvent>.Empty)
            .SortByDescending(x => x.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateIndexesAsync()
    {
        try
        {
            // Create index on TTL field for automatic cleanup
            var ttlIndex = Builders<SseOutboxEvent>.IndexKeys.Ascending(x => x.Ttl);
            var ttlOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.Zero };
            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<SseOutboxEvent>(ttlIndex, ttlOptions));
            
            // Index on SequenceNumber for ordered retrieval
            var sequenceIndex = Builders<SseOutboxEvent>.IndexKeys.Ascending(x => x.SequenceNumber);
            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<SseOutboxEvent>(sequenceIndex));
            
            // Index on CreatedAt for efficient querying
            var createdAtIndex = Builders<SseOutboxEvent>.IndexKeys.Ascending(x => x.CreatedAt);
            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<SseOutboxEvent>(createdAtIndex));
            
            _logger.LogInformation("Outbox event indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating outbox event indexes");
        }
    }
}