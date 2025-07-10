using MongoDB.Driver;
using SseRealTimeDemo.Models;

namespace SseDemo.Repositories;

/// <summary>
/// MongoDB implementation of the checkpoint repository
/// </summary>
public class CheckpointRepository : ICheckpointRepository
{
    private readonly IMongoCollection<ClientCheckpoint> _collection;
    private readonly ILogger<CheckpointRepository> _logger;

    public CheckpointRepository(IMongoClient mongoClient, IConfiguration configuration, ILogger<CheckpointRepository> logger)
    {
        _logger = logger;
        
        var databaseName = configuration["MongoDB:DatabaseName"] ?? "sse_outbox";
        var database = mongoClient.GetDatabase(databaseName);
        _collection = database.GetCollection<ClientCheckpoint>("client_checkpoints");
    }

    public async Task UpdateCheckpointAsync(string clientId, long sequenceNumber, string? eventId = null, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ClientCheckpoint>.Filter.Eq(c => c.ClientId, clientId);
        var update = Builders<ClientCheckpoint>.Update
            .Set(c => c.LastSequenceNumber, sequenceNumber)
            .Set(c => c.LastEventId, eventId)
            .Set(c => c.UpdatedAt, DateTime.UtcNow)
            .SetOnInsert(c => c.ClientId, clientId)
            .SetOnInsert(c => c.CreatedAt, DateTime.UtcNow);

        var options = new UpdateOptions { IsUpsert = true };

        try
        {
            await _collection.UpdateOneAsync(filter, update, options, cancellationToken);
            _logger.LogDebug("Updated checkpoint for client {ClientId} to sequence {SequenceNumber}", clientId, sequenceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update checkpoint for client {ClientId}", clientId);
            // Don't throw - checkpoint updates should not break event delivery
        }
    }

    public async Task<ClientCheckpoint?> GetCheckpointAsync(string clientId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ClientCheckpoint>.Filter.Eq(c => c.ClientId, clientId);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateIndexesAsync()
    {
        try
        {
            // Create index on ClientId for efficient lookups
            var clientIdIndex = Builders<ClientCheckpoint>.IndexKeys.Ascending(x => x.ClientId);
            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<ClientCheckpoint>(clientIdIndex));
            
            _logger.LogInformation("Checkpoint indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkpoint indexes");
        }
    }
}