import { useState } from 'react';
import { useSseTyped } from '../../hooks/useSseTyped';
import { SseConnectionStatus } from '../../services/sseService';
import { NotificationPayload, DataUpdatePayload } from '../../models/sseMessages';

interface TypedEventsExampleProps {
  clientId: string;
  backendUrl: string;
  apiKey?: string;
}

/**
 * Typed SSE events example component
 * Demonstrates using SSE with typed events and schema validation
 */
export default function TypedEventsExample({ clientId, backendUrl, apiKey }: TypedEventsExampleProps) {
  const [lastNotification, setLastNotification] = useState<NotificationPayload | null>(null);
  const [lastDataUpdate, setLastDataUpdate] = useState<DataUpdatePayload | null>(null);
  
  // Use the typed SSE hook
  const { 
    status, 
    notifications, 
    dataUpdates, 
    connect, 
    disconnect, 
    clearEvents 
  } = useSseTyped({
    url: backendUrl,
    clientId,
    autoConnect: true,
    apiKey,
    onNotification: (notification) => {
      console.log('Notification received:', notification);
      setLastNotification(notification);
    },
    onDataUpdate: (dataUpdate) => {
      console.log('Data update received:', dataUpdate);
      setLastDataUpdate(dataUpdate);
    }
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

  // Format severity for display
  const getSeverityClass = (severity: string) => {
    switch (severity) {
      case 'error':
        return 'severity-error';
      case 'warning':
        return 'severity-warning';
      default:
        return 'severity-info';
    }
  };

  return (
    <div className="example-container">
      <h2>Type-Safe Events with Validation</h2>
      <div className="example-purpose" style={{ 
        backgroundColor: '#e1f5fe', 
        padding: '1rem', 
        borderRadius: '8px', 
        marginBottom: '1rem',
        border: '1px solid #4fc3f7'
      }}>
        <h3>🎯 Purpose: Production-Ready Type Safety</h3>
        <p>
          <strong>What this demonstrates:</strong> Using TypeScript interfaces and Zod schema validation 
          to ensure events match expected formats. Prevents runtime errors from malformed data.
        </p>
        <p>
          <strong>Key features:</strong> TypeScript types, Zod validation, error boundaries, type-safe hooks
        </p>
        <p>
          <strong>Use this when:</strong> Building production apps where data integrity is critical
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

      <div className="latest-events">
        <h3>Latest Events</h3>
        
        <div className="latest-notification">
          <h4>Latest Notification</h4>
          {lastNotification ? (
            <div className={`notification ${getSeverityClass(lastNotification.severity)}`}>
              <div className="notification-header">
                <span className="notification-severity">{lastNotification.severity}</span>
                <span className="notification-time">{new Date(lastNotification.timestamp).toLocaleTimeString()}</span>
              </div>
              <div className="notification-message">{lastNotification.message}</div>
            </div>
          ) : (
            <p className="no-events">No notifications received yet.</p>
          )}
        </div>
        
        <div className="latest-data-update">
          <h4>Latest Data Update</h4>
          {lastDataUpdate ? (
            <div className="data-update">
              <div className="data-update-header">
                <span className="entity-type">{lastDataUpdate.entityType}</span>
                <span className="entity-id">{lastDataUpdate.entityId}</span>
                <span className="update-time">{new Date(lastDataUpdate.timestamp).toLocaleTimeString()}</span>
              </div>
              <pre className="data-update-changes">
                {JSON.stringify(lastDataUpdate.changes, null, 2)}
              </pre>
            </div>
          ) : (
            <p className="no-events">No data updates received yet.</p>
          )}
        </div>
      </div>

      <div className="events-lists">
        <div className="notifications-list">
          <h3>Notifications ({notifications.length})</h3>
          {notifications.length === 0 ? (
            <p className="no-events">No notifications received yet.</p>
          ) : (
            <ul className="events-list">
              {notifications.map((notification, index) => (
                <li key={index} className={`event-item ${getSeverityClass(notification.severity)}`}>
                  <div className="event-header">
                    <span className="event-severity">{notification.severity}</span>
                    <span className="event-time">{new Date(notification.timestamp).toLocaleTimeString()}</span>
                  </div>
                  <div className="event-message">{notification.message}</div>
                </li>
              ))}
            </ul>
          )}
        </div>
        
        <div className="data-updates-list">
          <h3>Data Updates ({dataUpdates.length})</h3>
          {dataUpdates.length === 0 ? (
            <p className="no-events">No data updates received yet.</p>
          ) : (
            <ul className="events-list">
              {dataUpdates.map((update, index) => (
                <li key={index} className="event-item">
                  <div className="event-header">
                    <span className="entity-type">{update.entityType}</span>
                    <span className="entity-id">{update.entityId}</span>
                    <span className="event-time">{new Date(update.timestamp).toLocaleTimeString()}</span>
                  </div>
                  <pre className="event-data">
                    {JSON.stringify(update.changes, null, 2)}
                  </pre>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      <div className="code-example">
        <h3>Code Example</h3>
        <pre>
{`import { useSseTyped } from '../hooks/useSseTyped';
import { NotificationPayload, DataUpdatePayload } from '../models/sseMessages';

function MyComponent() {
  const [lastNotification, setLastNotification] = useState<NotificationPayload | null>(null);
  const [lastDataUpdate, setLastDataUpdate] = useState<DataUpdatePayload | null>(null);
  
  const { 
    status, 
    notifications, 
    dataUpdates 
  } = useSseTyped({
    url: 'http://localhost:5121/api/sse/connect',
    clientId: 'my-client-id',
    autoConnect: true,
    apiKey: 'your-api-key', // Optional
    onNotification: (notification) => {
      console.log('Notification received:', notification);
      setLastNotification(notification);
    },
    onDataUpdate: (dataUpdate) => {
      console.log('Data update received:', dataUpdate);
      setLastDataUpdate(dataUpdate);
    }
  });

  return (
    <div>
      <p>Status: {status}</p>
      <h3>Latest Notification</h3>
      {lastNotification && (
        <div>
          <p>Severity: {lastNotification.severity}</p>
          <p>Message: {lastNotification.message}</p>
        </div>
      )}
    </div>
  );
}`}
        </pre>
      </div>
    </div>
  );
}
