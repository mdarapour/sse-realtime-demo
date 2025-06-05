// No useState needed
import { useSse } from '../../hooks/useSse';
import { SseConnectionStatus } from '../../services/sseService';

interface BasicExampleProps {
  clientId: string;
  backendUrl: string;
  apiKey?: string;
}

/**
 * Basic SSE example component
 * Demonstrates the simplest way to use SSE with the custom hook
 */
export default function BasicExample({ clientId, backendUrl, apiKey }: BasicExampleProps) {
  // Use the SSE hook with auto-connect
  const { status, events, connect, disconnect, clearEvents } = useSse({
    url: backendUrl,
    clientId,
    autoConnect: true,
    apiKey,
  });

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
      <h2>Basic SSE Example</h2>
      <div className="example-purpose" style={{ 
        backgroundColor: '#e3f2fd', 
        padding: '1rem', 
        borderRadius: '8px', 
        marginBottom: '1rem',
        border: '1px solid #90caf9'
      }}>
        <h3>🎯 Purpose: Getting Started with SSE</h3>
        <p>
          <strong>What this demonstrates:</strong> The simplest possible SSE implementation - 
          connect to a server and receive real-time events. Perfect for understanding the basics.
        </p>
        <p>
          <strong>Key features:</strong> Auto-connect on mount, receive all events, basic connection management
        </p>
        <p>
          <strong>Use this when:</strong> You need a simple real-time connection without complex filtering or typing
        </p>
      </div>

      <div className="connection-status">
        <span className={`status-indicator ${getStatusClass()}`}></span>
        <span>Connection Status: <strong>{status}</strong></span>
      </div>

      <div className="controls">
        <button onClick={connect} disabled={status === SseConnectionStatus.OPEN || status === SseConnectionStatus.CONNECTING}>
          Connect
        </button>
        <button onClick={disconnect} disabled={status !== SseConnectionStatus.OPEN}>
          Disconnect
        </button>
        <button onClick={clearEvents}>
          Clear Events
        </button>
      </div>

      <div className="events-container">
        <h3>Events Received ({events.length})</h3>
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

function MyComponent() {
  const { status, events } = useSse({
    url: 'http://localhost:5121/api/sse/connect',
    clientId: 'my-client-id',
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
