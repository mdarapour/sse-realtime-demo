import { useState } from 'react';
import { useSse } from '../../hooks/useSse';
import { SseConnectionStatus } from '../../services/sseService';
import { SseEventTypes } from '../../models/sseEventTypes';

interface FilteredEventsExampleProps {
  clientId: string;
  backendUrl: string;
  apiKey?: string;
}

// Event type information
const eventTypeInfo: Record<string, { automatic: boolean; description: string; example: string }> = {
  [SseEventTypes.Notification]: {
    automatic: false,
    description: 'User notifications - sent manually via API',
    example: 'System updates, user messages'
  },
  [SseEventTypes.Alert]: {
    automatic: false,
    description: 'Critical alerts - sent manually when issues occur',
    example: 'Security alerts, system failures'
  },
  [SseEventTypes.DataUpdate]: {
    automatic: false,
    description: 'Data changes - sent when entities are modified',
    example: 'User profile updates, config changes'
  },
  [SseEventTypes.Heartbeat]: {
    automatic: false,
    description: 'Connection keep-alive - can be sent periodically',
    example: 'Prevents timeout disconnections'
  }
};

/**
 * Filtered Events Example
 * Demonstrates how to filter events on the server side
 */
export default function FilteredEventsExample({ clientId, backendUrl, apiKey }: FilteredEventsExampleProps) {
  const [filter, setFilter] = useState('notification');
  const [isConnected, setIsConnected] = useState(false);
  const [isSending, setIsSending] = useState(false);

  // Use the SSE hook with auto-connect disabled
  const { status, events, connect: baseConnect, disconnect, clearEvents } = useSse({
    url: backendUrl,
    clientId: `${clientId}-filtered`,
    filter,
    autoConnect: false,
    apiKey,
  });

  // Send a test event of the selected type
  const sendTestEvent = async () => {
    if (!apiKey) {
      alert('API key is required to send events');
      return;
    }

    setIsSending(true);
    try {
      const baseApiUrl = backendUrl.replace('/connect', '');
      let endpoint = '';
      let body = {};

      switch (filter) {
        case SseEventTypes.Notification:
          endpoint = `${baseApiUrl}/notification`;
          body = {
            message: `Test notification sent at ${new Date().toLocaleTimeString()}`,
            severity: 'info'
          };
          break;
        case SseEventTypes.Alert:
          endpoint = `${baseApiUrl}/alert`;
          body = {
            message: `Test alert triggered at ${new Date().toLocaleTimeString()}`,
            severity: 'high',
            category: 'system'
          };
          break;
        case SseEventTypes.DataUpdate:
          endpoint = `${baseApiUrl}/data-update`;
          body = {
            entityId: 'test-123',
            entityType: 'test',
            changes: { timestamp: new Date().toISOString() }
          };
          break;
        case SseEventTypes.Heartbeat:
          endpoint = `${baseApiUrl}/broadcast`;
          body = {
            eventType: 'heartbeat',
            data: JSON.stringify({ timestamp: new Date().toISOString() })
          };
          break;
      }

      const response = await fetch(endpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-API-Key': apiKey
        },
        body: JSON.stringify(body)
      });

      if (!response.ok) {
        throw new Error(`Failed to send event: ${response.statusText}`);
      }
    } catch (error) {
      console.error('Error sending test event:', error);
      alert('Failed to send test event. Check console for details.');
    } finally {
      setIsSending(false);
    }
  };

  // Connect with the current filter
  const connect = () => {
    baseConnect();
    setIsConnected(true);
  };

  // Disconnect and allow changing the filter
  const handleDisconnect = () => {
    disconnect();
    setIsConnected(false);
  };

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
      <h2>Filtered Events Example</h2>
      <div className="example-purpose" style={{ 
        backgroundColor: '#f3e5f5', 
        padding: '1rem', 
        borderRadius: '8px', 
        marginBottom: '1rem',
        border: '1px solid #ce93d8'
      }}>
        <h3>🎯 Purpose: Server-Side Event Filtering</h3>
        <p>
          <strong>What this demonstrates:</strong> How to request only specific event types from the server, 
          reducing bandwidth and processing overhead by filtering events before they're sent.
        </p>
        <p>
          <strong>Key features:</strong> Event type selection, server-side filtering, bandwidth optimization
        </p>
        <p>
          <strong>Use this when:</strong> You only need specific event types and want to reduce network traffic
        </p>
      </div>
      <div className="info-box" style={{ marginTop: '1rem', padding: '1rem', backgroundColor: '#fff3cd', borderRadius: '4px', border: '1px solid #ffc107' }}>
        <strong>⚠️ Note:</strong> Events must be triggered manually using the "Send Test Event" button or via the API.
        To see automatic events, start the demo mode: <code>POST /api/demo/start {"{"}"intervalSeconds": 3{"}"}</code>
      </div>

      <div className="connection-status">
        <span className={`status-indicator ${getStatusClass()}`}></span>
        <span>Connection Status: <strong>{status}</strong></span>
      </div>

      <div className="filter-controls">
        <label htmlFor="event-filter">Event Type Filter:</label>
        <select
          id="event-filter"
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
          disabled={isConnected}
        >
          <option value={SseEventTypes.Notification}>{SseEventTypes.Notification}</option>
          <option value={SseEventTypes.Alert}>{SseEventTypes.Alert}</option>
          <option value={SseEventTypes.DataUpdate}>{SseEventTypes.DataUpdate}</option>
          <option value={SseEventTypes.Heartbeat}>{SseEventTypes.Heartbeat}</option>
        </select>
        {eventTypeInfo[filter] && (
          <div className="event-type-info" style={{ marginTop: '0.5rem', fontSize: '0.9em', color: '#666' }}>
            <p><strong>Description:</strong> {eventTypeInfo[filter].description}</p>
            <p><strong>Example:</strong> {eventTypeInfo[filter].example}</p>
            <p><strong>Automatic:</strong> {eventTypeInfo[filter].automatic ? 'Yes' : 'No - Must be triggered manually'}</p>
          </div>
        )}
        <p className="filter-note">
          <strong>Note:</strong> You can only change the filter when disconnected.
          The filter is applied on the server side.
        </p>
      </div>

      <div className="controls">
        <button
          onClick={connect}
          disabled={status === SseConnectionStatus.OPEN || status === SseConnectionStatus.CONNECTING}
        >
          Connect with Filter
        </button>
        <button
          onClick={handleDisconnect}
          disabled={status !== SseConnectionStatus.OPEN}
        >
          Disconnect
        </button>
        <button onClick={clearEvents}>
          Clear Events
        </button>
        <button
          onClick={sendTestEvent}
          disabled={status !== SseConnectionStatus.OPEN || isSending}
          style={{ marginLeft: '0.5rem', backgroundColor: '#4CAF50', color: 'white' }}
        >
          {isSending ? 'Sending...' : `Send Test ${filter} Event`}
        </button>
      </div>

      <div className="events-container">
        <h3>Filtered Events Received ({events.length})</h3>
        {events.length === 0 ? (
          <p className="no-events">No events received yet.</p>
        ) : (
          <ul className="events-list">
            {events.map((event, index) => (
              <li key={index} className={`event-item ${event.event}`}>
                <div className="event-header">
                  <span className="event-type">{event.event || 'message'}</span>
                  <span className="event-id">{event.id ? `ID: ${event.id}` : 'No ID'}</span>
                </div>
                <pre className="event-data">{event.data}</pre>
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="code-example">
        <h3>Code Example</h3>
        <pre>
{`import { useSse } from '../hooks/useSse.js';

function FilteredComponent() {
  const { status, events } = useSse({
    url: 'http://localhost:5121/api/sse/connect',
    clientId: 'my-client-id',
    filter: 'notification', // Only receive notification events
    autoConnect: true,
    apiKey: 'your-api-key', // Optional
  });

  return (
    <div>
      <p>Status: {status}</p>
      <ul>
        {events.map((event, index) => (
          <li key={index}>
            {event.event}: {event.data}
          </li>
        ))}
      </ul>
    </div>
  );
}`}
        </pre>
      </div>
    </div>
  );
}