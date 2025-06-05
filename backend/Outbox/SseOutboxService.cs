using MongoDB.Driver;
using MongoDB.Bson;
using SseDemo.Models;
using SseDemo.Outbox.Models;
using SseDemo.Services;
using System.Collections.Concurrent;

namespace SseDemo.Outbox;

/// <summary>
/// Service that handles SSE events through MongoDB outbox pattern
/// </summary>
public class SseOutboxService : BackgroundService
{
    private readonly ILogger<SseOutboxService> _logger;
    private readonly IMongoCollection<SseOutboxEvent> _outboxCollection;
    private readonly SseService _sseService;
    private readonly string _instanceId;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, DateTime> _lastProcessedTimes = new();

    public SseOutboxService(
        ILogger<SseOutboxService> logger,
        IMongoClient mongoClient,
        SseService sseService,
        IConfiguration configuration)
    {
        _logger = logger;
        _sseService = sseService;
        _configuration = configuration;
        _instanceId = $"{Environment.MachineName}-{Guid.NewGuid():N}";
        
        var databaseName = _configuration["MongoDB:DatabaseName"] ?? "sse_outbox";
        var collectionName = _configuration["MongoDB:OutboxCollection"] ?? "outbox_events";
        
        var database = mongoClient.GetDatabase(databaseName);
        _outboxCollection = database.GetCollection<SseOutboxEvent>(collectionName);
        
        // Create TTL index for automatic cleanup
        CreateIndexes();
    }

    private void CreateIndexes()
    {
        try
        {
            // TTL index to automatically delete old events
            var ttlIndex = Builders<SseOutboxEvent>.IndexKeys.Ascending(x => x.Ttl);
            var ttlOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.Zero };
            _outboxCollection.Indexes.CreateOne(new CreateIndexModel<SseOutboxEvent>(ttlIndex, ttlOptions));
            
            // Index on CreatedAt for efficient querying
            var createdAtIndex = Builders<SseOutboxEvent>.IndexKeys.Ascending(x => x.CreatedAt);
            _outboxCollection.Indexes.CreateOne(new CreateIndexModel<SseOutboxEvent>(createdAtIndex));
            
            _logger.LogInformation("MongoDB indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating MongoDB indexes");
        }
    }

    /// <summary>
    /// Publishes an event to the outbox
    /// </summary>
    public async Task PublishEventAsync(SseEvent sseEvent, string? targetClientId = null)
    {
        var outboxEvent = new SseOutboxEvent
        {
            EventId = sseEvent.Id ?? Guid.NewGuid().ToString(),
            EventType = sseEvent.Event ?? "message",
            EventData = sseEvent.Data ?? string.Empty,
            TargetClientId = targetClientId,
            CreatedAt = DateTime.UtcNow
        };
        
        await _outboxCollection.InsertOneAsync(outboxEvent);
        _logger.LogDebug("Published event {EventId} to outbox", outboxEvent.EventId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting SseOutboxService on instance {InstanceId}", _instanceId);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxEvents(stoppingToken);
                await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected when shutting down
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox events");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        
        _logger.LogInformation("Stopping SseOutboxService on instance {InstanceId}", _instanceId);
    }

    private async Task ProcessOutboxEvents(CancellationToken cancellationToken)
    {
        // Get the last processed time for this instance
        var lastProcessed = _lastProcessedTimes.GetOrAdd(_instanceId, DateTime.MinValue);
        
        // Query for new events since last check
        var filter = Builders<SseOutboxEvent>.Filter.And(
            Builders<SseOutboxEvent>.Filter.Gt(x => x.CreatedAt, lastProcessed),
            Builders<SseOutboxEvent>.Filter.Eq(x => x.ProcessedAt, null)
        );
        
        var sort = Builders<SseOutboxEvent>.Sort.Ascending(x => x.CreatedAt);
        
        var cursor = await _outboxCollection.FindAsync(filter, new FindOptions<SseOutboxEvent>
        {
            Sort = sort,
            BatchSize = 100
        }, cancellationToken);
        
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var outboxEvent in cursor.Current)
            {
                try
                {
                    await ProcessSingleEvent(outboxEvent, cancellationToken);
                    
                    // Update last processed time
                    _lastProcessedTimes[_instanceId] = outboxEvent.CreatedAt;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing event {EventId}", outboxEvent.EventId);
                }
            }
        }
    }

    private async Task ProcessSingleEvent(SseOutboxEvent outboxEvent, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing event {EventId} of type {EventType}", 
            outboxEvent.EventId, outboxEvent.EventType);
        
        // Convert back to SseEvent
        var sseEvent = new SseEvent
        {
            Id = outboxEvent.EventId,
            Event = outboxEvent.EventType,
            Data = outboxEvent.EventData
        };
        
        // Send to appropriate clients
        if (string.IsNullOrEmpty(outboxEvent.TargetClientId))
        {
            // Broadcast to all local clients only (not back to outbox)
            _logger.LogDebug("Delivering event {EventId} of type {EventType} to local clients", 
                sseEvent.Id, sseEvent.Event);
            _sseService.DeliverEventToLocalClients(sseEvent);
        }
        else
        {
            // Send to specific client if connected locally
            _logger.LogDebug("Delivering event {EventId} of type {EventType} to client {ClientId}", 
                sseEvent.Id, sseEvent.Event, outboxEvent.TargetClientId);
            _sseService.SendEventToClient(outboxEvent.TargetClientId, sseEvent);
        }
        
        // Mark as processed
        var update = Builders<SseOutboxEvent>.Update
            .Set(x => x.ProcessedAt, DateTime.UtcNow)
            .Set(x => x.ProcessedBy, _instanceId);
            
        await _outboxCollection.UpdateOneAsync(
            x => x.Id == outboxEvent.Id,
            update,
            cancellationToken: cancellationToken);
    }
}