import { useState, useEffect } from 'react';
import { useSse } from '../../hooks/useSse';
import { SseConnectionStatus } from '../../services/sseService';

interface ReconnectionExampleProps {
  clientId: string;
  backendUrl: string;
  apiKey?: string;
}

/**
 * Reconnection Example
 * Demonstrates automatic and manual reconnection strategies
 */
export default function ReconnectionExample({ clientId, backendUrl, apiKey }: ReconnectionExampleProps) {
  const [retryTimeout, setRetryTimeout] = useState(3000);
  const [maxRetries, setMaxRetries] = useState(5);
  const [autoReconnect, setAutoReconnect] = useState(true);
  const [connectionLogs, setConnectionLogs] = useState<string[]>([]);
  const [isConnected, setIsConnected] = useState(false);

  // Use the SSE hook with custom reconnection settings
  const { status, connect: baseConnect, disconnect } = useSse({
    url: backendUrl,
    clientId: `${clientId}-reconnect`,
    retryTimeout,
    maxRetryAttempts: maxRetries,
    autoReconnect,
    autoConnect: false,
    apiKey,
  });

  // Connect with current settings
  const connect = () => {
    addLog(`Connecting with settings: retryTimeout=${retryTimeout}ms, maxRetries=${maxRetries}, autoReconnect=${autoReconnect}`);
    baseConnect();
    setIsConnected(true);
  };

  // Disconnect and allow changing settings
  const handleDisconnect = () => {
    addLog('Manually disconnected');
    disconnect();
    setIsConnected(false);
  };

  // Simulate a server error
  const simulateServerError = () => {
    addLog('Simulating server error...');
    // In a real application, you would trigger a server error
    // For this demo, we'll just disconnect and reconnect
    disconnect();
    setTimeout(() => {
      if (autoReconnect) {
        addLog('Auto-reconnecting after simulated error');
        connect();
      } else {
        addLog('Auto-reconnect disabled. Connection remains closed.');
      }
    }, 1000);
  };

  // Add a log entry
  const addLog = (message: string) => {
    const timestamp = new Date().toISOString().substring(11, 23);
    setConnectionLogs(prev => [`[${timestamp}] ${message}`, ...prev.slice(0, 19)]);
  };

  // Log status changes
  useEffect(() => {
    addLog(`Connection status changed to: ${status}`);
  }, [status]);

  // Format the connection status
  const getStatusClass = () => {
    switch (status) {
      case SseConnectionStatus.OPEN:
        return 'status-success';
      case SseConnectionStatus.CONNECTING:
        return 'status-warning';
      case SseConnectionStatus.ERROR:
        return 'status-error';
      default:
        return 'status-default';
    }
  };

  return (
    <div className="example-container">
      <h2>Reconnection Strategies Example</h2>
      <div className="example-purpose" style={{ 
        backgroundColor: '#fff3e0', 
        padding: '1rem', 
        borderRadius: '8px', 
        marginBottom: '1rem',
        border: '1px solid #ffb74d'
      }}>
        <h3>🎯 Purpose: Connection Resilience & Error Recovery</h3>
        <p>
          <strong>What this demonstrates:</strong> Handling connection failures gracefully with automatic 
          reconnection, configurable retry strategies, and connection state monitoring.
        </p>
        <p>
          <strong>Key features:</strong> Auto-reconnect, configurable retry logic, connection logs, error simulation
        </p>
        <p>
          <strong>Use this when:</strong> Building production apps that need to handle network issues gracefully
        </p>
      </div>

      <div className="connection-status">
        <span className={`status-indicator ${getStatusClass()}`}></span>
        <span>Connection Status: <strong>{status}</strong></span>
      </div>

      <div className="reconnection-settings">
        <h3>Reconnection Settings</h3>
        <div className="settings-form">
          <div className="form-group">
            <label htmlFor="retry-timeout">Retry Timeout (ms):</label>
            <input
              id="retry-timeout"
              type="number"
              min="1000"
              max="10000"
              step="1000"
              value={retryTimeout}
              onChange={(e) => setRetryTimeout(Number(e.target.value))}
              disabled={isConnected}
            />
          </div>

          <div className="form-group">
            <label htmlFor="max-retries">Max Retry Attempts:</label>
            <input
              id="max-retries"
              type="number"
              min="1"
              max="10"
              value={maxRetries}
              onChange={(e) => setMaxRetries(Number(e.target.value))}
              disabled={isConnected}
            />
          </div>

          <div className="form-group">
            <label htmlFor="auto-reconnect">Auto Reconnect:</label>
            <input
              id="auto-reconnect"
              type="checkbox"
              checked={autoReconnect}
              onChange={(e) => setAutoReconnect(e.target.checked)}
              disabled={isConnected}
            />
          </div>
        </div>
      </div>

      <div className="controls">
        <button
          onClick={connect}
          disabled={status === SseConnectionStatus.OPEN || status === SseConnectionStatus.CONNECTING}
        >
          Connect
        </button>
        <button
          onClick={handleDisconnect}
          disabled={status !== SseConnectionStatus.OPEN}
        >
          Disconnect
        </button>
        <button
          onClick={simulateServerError}
          disabled={status !== SseConnectionStatus.OPEN}
        >
          Simulate Server Error
        </button>
        <button onClick={() => setConnectionLogs([])}>
          Clear Logs
        </button>
      </div>

      <div className="connection-logs">
        <h3>Connection Logs</h3>
        <div className="logs-container">
          {connectionLogs.length === 0 ? (
            <p className="no-logs">No connection logs yet.</p>
          ) : (
            <ul className="logs-list">
              {connectionLogs.map((log, index) => (
                <li key={index} className="log-entry">{log}</li>
              ))}
            </ul>
          )}
        </div>
      </div>

      <div className="code-example">
        <h3>Code Example</h3>
        <pre>
{`import { useSse } from '../hooks/useSse.js';

function ReconnectionComponent() {
  const { status, events, connect, disconnect } = useSse({
    url: '/api/sse/connect',
    clientId: 'my-client-id',
    retryTimeout: 3000,       // Retry after 3 seconds
    maxRetryAttempts: 5,      // Try up to 5 times
    autoReconnect: true,      // Automatically reconnect
    autoConnect: true,        // Connect when component mounts
    apiKey: 'your-api-key',   // Optional
  });

  return (
    <div>
      <p>Status: {status}</p>
      <button onClick={disconnect}>Disconnect</button>
      <button onClick={connect}>Reconnect</button>
    </div>
  );
}`}
        </pre>
      </div>
    </div>
  );
}
