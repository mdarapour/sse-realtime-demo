// No useState needed
import { useSse } from '../../hooks/useSse';
import { SseConnectionStatus } from '../../services/sseService';
import { SseEventTypes } from '../../models/sseEventTypes';

interface MultipleStreamsExampleProps {
  clientId: string;
  backendUrl: string;
  apiKey?: string;
}

/**
 * Multiple Streams Example
 * Demonstrates how to use multiple SSE connections simultaneously
 */
export default function MultipleStreamsExample({ clientId, backendUrl, apiKey }: MultipleStreamsExampleProps) {
  // Create three separate SSE connections with different filters
  const notificationStream = useSse({
    url: backendUrl,
    clientId: `${clientId}-notifications`,
    filter: SseEventTypes.Notification,
    autoConnect: false,
    apiKey,
  });

  const alertStream = useSse({
    url: backendUrl,
    clientId: `${clientId}-alerts`,
    filter: SseEventTypes.Alert,
    autoConnect: false,
    apiKey,
  });

  const updateStream = useSse({
    url: backendUrl,
    clientId: `${clientId}-updates`,
    filter: 'update', // Keep legacy filter for demonstration
    autoConnect: false,
    apiKey,
  });

  // Connect all streams
  const connectAll = () => {
    notificationStream.connect();
    alertStream.connect();
    updateStream.connect();
  };

  // Disconnect all streams
  const disconnectAll = () => {
    notificationStream.disconnect();
    alertStream.disconnect();
    updateStream.disconnect();
  };

  // Clear all events
  const clearAll = () => {
    notificationStream.clearEvents();
    alertStream.clearEvents();
    updateStream.clearEvents();
  };

  // Format the connection status
  const getStatusClass = (status: SseConnectionStatus) => {
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
      <h2>Multiple Streams Example</h2>
      <div className="example-purpose" style={{ 
        backgroundColor: '#e8f5e9', 
        padding: '1rem', 
        borderRadius: '8px', 
        marginBottom: '1rem',
        border: '1px solid #81c784'
      }}>
        <h3>🎯 Purpose: Parallel Event Streams</h3>
        <p>
          <strong>What this demonstrates:</strong> Managing multiple concurrent SSE connections, 
          each filtering different event types. Shows how to organize complex real-time data flows.
        </p>
        <p>
          <strong>Key features:</strong> Multiple connections, independent stream management, organized event display
        </p>
        <p>
          <strong>Use this when:</strong> You need to separate concerns and handle different event types independently
        </p>
      </div>

      <div className="controls">
        <button
          onClick={connectAll}
          disabled={
            notificationStream.status === SseConnectionStatus.OPEN ||
            alertStream.status === SseConnectionStatus.OPEN ||
            updateStream.status === SseConnectionStatus.OPEN
          }
        >
          Connect All Streams
        </button>
        <button
          onClick={disconnectAll}
          disabled={
            notificationStream.status !== SseConnectionStatus.OPEN &&
            alertStream.status !== SseConnectionStatus.OPEN &&
            updateStream.status !== SseConnectionStatus.OPEN
          }
        >
          Disconnect All
        </button>
        <button onClick={clearAll}>
          Clear All Events
        </button>
      </div>

      <div className="streams-container">
        <div className="stream-column">
          <h3>Notifications</h3>
          <div className="connection-status">
            <span className={`status-indicator ${getStatusClass(notificationStream.status)}`}></span>
            <span>Status: <strong>{notificationStream.status}</strong></span>
          </div>
          <div className="events-list">
            {notificationStream.events.length === 0 ? (
              <p className="no-events">No notifications yet.</p>
            ) : (
              <ul>
                {notificationStream.events.map((event, index) => (
                  <li key={index} className="event-item notification">
                    <pre className="event-data">{event.data}</pre>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>

        <div className="stream-column">
          <h3>Alerts</h3>
          <div className="connection-status">
            <span className={`status-indicator ${getStatusClass(alertStream.status)}`}></span>
            <span>Status: <strong>{alertStream.status}</strong></span>
          </div>
          <div className="events-list">
            {alertStream.events.length === 0 ? (
              <p className="no-events">No alerts yet.</p>
            ) : (
              <ul>
                {alertStream.events.map((event, index) => (
                  <li key={index} className="event-item alert">
                    <pre className="event-data">{event.data}</pre>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>

        <div className="stream-column">
          <h3>Updates</h3>
          <div className="connection-status">
            <span className={`status-indicator ${getStatusClass(updateStream.status)}`}></span>
            <span>Status: <strong>{updateStream.status}</strong></span>
          </div>
          <div className="events-list">
            {updateStream.events.length === 0 ? (
              <p className="no-events">No updates yet.</p>
            ) : (
              <ul>
                {updateStream.events.map((event, index) => (
                  <li key={index} className="event-item update">
                    <pre className="event-data">{event.data}</pre>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
      </div>

      <div className="code-example">
        <h3>Code Example</h3>
        <pre>
{`import { useSse } from '../hooks/useSse.js';
import { SseEventTypes } from '../models/sseEventTypes';

function MultipleStreamsComponent() {
  // Create separate connections for different event types
  const notifications = useSse({
    url: '/api/sse/connect',
    clientId: 'client-notifications',
    filter: SseEventTypes.Notification,
    apiKey: 'your-api-key', // Optional
  });

  const alerts = useSse({
    url: '/api/sse/connect',
    clientId: 'client-alerts',
    filter: SseEventTypes.Alert,
    apiKey: 'your-api-key', // Optional
  });

  const updates = useSse({
    url: '/api/sse/connect',
    clientId: 'client-updates',
    filter: SseEventTypes.DataUpdate,
    apiKey: 'your-api-key', // Optional
  });

  return (
    <div className="dashboard">
      <div className="notifications-panel">
        <h3>Notifications ({notifications.events.length})</h3>
        {/* Render notification events */}
      </div>

      <div className="alerts-panel">
        <h3>Alerts ({alerts.events.length})</h3>
        {/* Render alert events */}
      </div>

      <div className="updates-panel">
        <h3>Updates ({updates.events.length})</h3>
        {/* Render update events */}
      </div>
    </div>
  );
}`}
        </pre>
      </div>
    </div>
  );
}
