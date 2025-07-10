using SseDemo.Application.Services;
using SseDemo.Models;
using SseDemo.Services;

namespace SseDemo.Infrastructure.Services;

/// <summary>
/// Implementation of connection management service
/// </summary>
public class ConnectionManagementService : IConnectionManagementService
{
    private readonly ISseService _sseService;
    private readonly ILogger<ConnectionManagementService> _logger;

    public ConnectionManagementService(
        ISseService sseService,
        ILogger<ConnectionManagementService> logger)
    {
        _sseService = sseService;
        _logger = logger;
    }

    public IAsyncEnumerable<string> EstablishConnectionAsync(
        string clientId, 
        string? filter = null, 
        long? checkpoint = null, 
        string? lastEventId = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Establishing SSE connection for client {ClientId} with filter {Filter}", 
                clientId, filter ?? "none");

            // Use existing SSE service - we could add business logic here like:
            // - Validation of client permissions
            // - Rate limiting checks
            // - Connection logging/auditing
            // - Custom connection policies

            if (checkpoint.HasValue || !string.IsNullOrEmpty(lastEventId))
            {
                _logger.LogInformation("Connection includes checkpoint recovery: checkpoint={Checkpoint}, lastEventId={LastEventId}", 
                    checkpoint, lastEventId);
                
                return FormatSseEvents(_sseService.GetSseEventsAsync(clientId, filter, checkpoint, lastEventId, cancellationToken));
            }

            return FormatSseEvents(_sseService.GetSseEventsAsync(clientId, filter, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish connection for client {ClientId}", clientId);
            throw;
        }
    }

    public async Task DisconnectClientAsync(string clientId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Disconnecting client {ClientId}", clientId);
            
            // Use existing SSE service method
            _sseService.UnregisterClient(clientId);
            
            _logger.LogInformation("Successfully disconnected client {ClientId}", clientId);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect client {ClientId}", clientId);
            throw;
        }
    }

    public async Task<ConnectionStatus> GetConnectionStatusAsync(string clientId, CancellationToken cancellationToken = default)
    {
        try
        {
            // This is a simplified implementation
            // In a real scenario, we might track more detailed connection metadata
            var isConnected = _sseService.IsClientConnected(clientId);
            
            return new ConnectionStatus
            {
                ClientId = clientId,
                IsConnected = isConnected,
                ConnectedAt = isConnected ? DateTime.UtcNow : null // This would be tracked properly in real implementation
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get connection status for client {ClientId}", clientId);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetConnectedClientsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Use existing SSE service method
            return _sseService.GetConnectedClients();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get connected clients");
            throw;
        }
    }

    public async Task<ConnectionStatistics> GetConnectionStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var connectedClients = _sseService.GetConnectedClients().ToList();
            
            return new ConnectionStatistics
            {
                TotalConnections = connectedClients.Count(),
                ActiveConnections = connectedClients.Count(),
                ConnectionsByFilter = new Dictionary<string, int>(), // Would need to track filters in real implementation
                OldestConnection = connectedClients.Any() ? DateTime.UtcNow : null // Would track actual connection times
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get connection statistics");
            throw;
        }
    }

    private async IAsyncEnumerable<string> FormatSseEvents(IAsyncEnumerable<SseEvent> events)
    {
        await foreach (var sseEvent in events)
        {
            yield return sseEvent.Format();
        }
    }
}