import { useState } from 'react';
import { useSse } from '../../hooks/useSse';
import { SseConnectionStatus } from '../../services/sseService';
import { SseEventTypes } from '../../models/sseEventTypes';

interface CustomEventsExampleProps {
  clientId: string;
  backendUrl: string;
  apiKey?: string;
}

/**
 * Custom Events Example
 * Demonstrates how to send and receive custom events
 */
export default function CustomEventsExample({ clientId, backendUrl, apiKey }: CustomEventsExampleProps) {
  const [customEventType, setCustomEventType] = useState<string>(SseEventTypes.Message);
  const [customEventData, setCustomEventData] = useState('{"text": "Hello from the client!"}');
  
  // Use the basic SSE hook
  const { status, events, connect, disconnect, clearEvents } = useSse({
    url: backendUrl,
    clientId: `${clientId}-custom`,
    autoConnect: true,
    apiKey,
  });

  // Send a custom event using the broadcast endpoint
  const sendCustomEvent = async () => {
    try {
      // Validate JSON
      try {
        JSON.parse(customEventData);
      } catch (e) {
        alert('Please enter valid JSON');
        return;
      }

      const headers: HeadersInit = {
        'Content-Type': 'application/json',
      };
      if (apiKey) {
        headers['X-API-Key'] = apiKey;
      }

      const response = await fetch('/api/sse/broadcast', {
        method: 'POST',
        headers,
        body: JSON.stringify({
          eventType: customEventType,
          data: customEventData,
        }),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      console.log(`Sent ${customEventType} event successfully`);
    } catch (error) {
      console.error('Error sending custom event:', error);
      alert(`Error sending event: ${error}`);
    }
  };

  // Send a typed event using the specific endpoints
  const sendTypedEvent = async (type: string) => {
    try {
      const headers: HeadersInit = {
        'Content-Type': 'application/json',
      };
      if (apiKey) {
        headers['X-API-Key'] = apiKey;
      }

      let endpoint = '';
      let body = {};

      switch (type) {
        case 'notification':
          endpoint = '/api/sse/notification';
          body = {
            message: `Test notification sent at ${new Date().toLocaleTimeString()}`,
            severity: 'info'
          };
          break;
        case 'alert':
          endpoint = '/api/sse/alert';
          body = {
            message: `Test alert sent at ${new Date().toLocaleTimeString()}`,
            severity: 'high',
            category: 'system'
          };
          break;
        case 'dataUpdate':
          endpoint = '/api/sse/data-update';
          body = {
            entityId: 'test-123',
            entityType: 'test',
            changes: { 
              status: 'updated',
              timestamp: new Date().toISOString() 
            }
          };
          break;
      }

      const response = await fetch(endpoint, {
        method: 'POST',
        headers,
        body: JSON.stringify(body),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      console.log(`Sent ${type} event successfully`);
    } catch (error) {
      console.error(`Error sending ${type} event:`, error);
      alert(`Error sending ${type} event: ${error}`);
    }
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

  // Group events by type
  const eventsByType = events.reduce((acc, event) => {
    const type = event.event || 'message';
    if (!acc[type]) {
      acc[type] = [];
    }
    acc[type].push(event);
    return acc;
  }, {} as Record<string, typeof events>);

  return (
    <div className="example-container">
      <h2>Custom Events Example</h2>
      <div className="example-purpose" style={{ 
        backgroundColor: '#fce4ec', 
        padding: '1rem', 
        borderRadius: '8px', 
        marginBottom: '1rem',
        border: '1px solid #f06292'
      }}>
        <h3>🎯 Purpose: Broadcasting Events to Other Clients</h3>
        <p>
          <strong>What this demonstrates:</strong> How to send events from one client to all connected clients. 
          Shows both custom JSON events and typed events using dedicated endpoints.
        </p>
        <p>
          <strong>Key features:</strong> Send custom events, broadcast to all clients, typed event endpoints
        </p>
        <p>
          <strong>Use this when:</strong> Building collaborative features, notifications, or real-time updates
        </p>
      </div>

      <div className="connection-status">
        <span className={`status-indicator ${getStatusClass()}`}></span>
        <span>Connection Status: <strong>{status}</strong></span>
      </div>

      <div className="controls">
        <button
          onClick={connect}
          disabled={status === SseConnectionStatus.OPEN || status === SseConnectionStatus.CONNECTING}
        >
          Connect
        </button>
        <button
          onClick={disconnect}
          disabled={status !== SseConnectionStatus.OPEN}
        >
          Disconnect
        </button>
        <button onClick={clearEvents}>
          Clear Events
        </button>
      </div>

      <div className="custom-event-section">
        <h3>Send Custom Event</h3>
        
        <div className="typed-event-buttons">
          <h4>Quick Actions (Typed Events)</h4>
          <button onClick={() => sendTypedEvent('notification')}>
            Send Notification
          </button>
          <button onClick={() => sendTypedEvent('alert')}>
            Send Alert
          </button>
          <button onClick={() => sendTypedEvent('dataUpdate')}>
            Send Data Update
          </button>
        </div>

        <div className="custom-event-form">
          <h4>Custom Event (JSON)</h4>
          <div className="form-group">
            <label htmlFor="event-type">Event Type:</label>
            <select
              id="event-type"
              value={customEventType}
              onChange={(e) => setCustomEventType(e.target.value)}
            >
              <option value={SseEventTypes.Message}>message</option>
              <option value={SseEventTypes.Notification}>notification</option>
              <option value={SseEventTypes.Alert}>alert</option>
              <option value={SseEventTypes.DataUpdate}>dataUpdate</option>
              <option value={SseEventTypes.Heartbeat}>heartbeat</option>
              <option value="custom">custom</option>
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="event-data">Event Data (JSON):</label>
            <textarea
              id="event-data"
              value={customEventData}
              onChange={(e) => setCustomEventData(e.target.value)}
              rows={4}
              placeholder='{"text": "Your message here"}'
            />
          </div>

          <button onClick={sendCustomEvent}>Send Custom Event</button>
        </div>
      </div>

      <div className="events-display">
        <h3>All Events Received ({events.length})</h3>
        
        {Object.entries(eventsByType).map(([type, typeEvents]) => (
          <div key={type} className="event-type-section">
            <h4>{type} Events ({typeEvents.length})</h4>
            {typeEvents.length === 0 ? (
              <p className="no-events">No {type} events received.</p>
            ) : (
              <ul className="events-list">
                {typeEvents.slice(-10).map((event, index) => (
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
        ))}
      </div>

      <div className="code-example">
        <h3>Code Example</h3>
        <pre>
{`// Send a custom event
async function sendCustomEvent(eventType, eventData) {
  const response = await fetch('/api/sse/broadcast', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-API-Key': 'your-api-key'
    },
    body: JSON.stringify({
      eventType: eventType,
      data: JSON.stringify(eventData)
    })
  });
  
  if (!response.ok) {
    throw new Error('Failed to send event');
  }
}

// Send typed events using specific endpoints
async function sendNotification(message) {
  const response = await fetch('/api/sse/notification', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-API-Key': 'your-api-key'
    },
    body: JSON.stringify({
      message: message,
      severity: 'info'
    })
  });
}

// Receive all events
const { status, events } = useSse({
  url: '/api/sse/connect',
  clientId: 'my-client-id',
  autoConnect: true,
  apiKey: 'your-api-key'
});`}
        </pre>
      </div>
    </div>
  );
}