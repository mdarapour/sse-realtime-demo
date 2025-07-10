import { useState, useCallback, useEffect } from 'react';
import { useSse } from '../../hooks/useSse';
import { SseConnectionStatus } from '../../services/sseService';

interface CheckpointRecoveryExampleProps {
  clientId: string;
  backendUrl: string;
  apiKey?: string;
}

interface EventData {
  id: string;
  data: any;
  timestamp: string;
  sequenceNumber?: number;
  recovered?: boolean;
}

/**
 * Checkpoint Recovery Example
 * Demonstrates how checkpoint recovery works on reconnection
 */
export default function CheckpointRecoveryExample({ clientId, backendUrl, apiKey }: CheckpointRecoveryExampleProps) {
  const [useCheckpoint, setUseCheckpoint] = useState(true);
  const [eventLog, setEventLog] = useState<EventData[]>([]);
  const [checkpointInfo, setCheckpointInfo] = useState<{ sequence: number | null; eventId: string | null }>({
    sequence: null,
    eventId: null
  });
  const [missedEventCount, setMissedEventCount] = useState(0);

  // Use the SSE hook with checkpoint settings
  const { 
    status, 
    lastEvent,
    connect: baseConnect, 
    disconnect,
    clearEvents,
    sseClient 
  } = useSse({
    url: backendUrl,
    clientId: `${clientId}-checkpoint`,
    useCheckpoint,
    checkpointStorageKey: `sse-checkpoint-demo`,
    autoConnect: false,
    apiKey,
  });

  // Process incoming events
  useEffect(() => {
    if (lastEvent) {
      try {
        const data = JSON.parse(lastEvent.data);
        const sequenceNumber = data._sequence;
        
        // Update checkpoint info
        if (sequenceNumber !== undefined) {
          setCheckpointInfo(prev => ({
            sequence: sequenceNumber,
            eventId: lastEvent.id || prev.eventId
          }));
        }

        // Add to event log
        const eventData: EventData = {
          id: lastEvent.id || `event-${Date.now()}`,
          data,
          timestamp: new Date().toISOString(),
          sequenceNumber,
          recovered: false
        };

        setEventLog(prev => [eventData, ...prev].slice(0, 50));
      } catch (error) {
        console.error('Error parsing event data:', error);
      }
    }
  }, [lastEvent]);

  // Connect with checkpoint logging
  const connect = useCallback(() => {
    const lastSequence = sseClient?.getLastSequenceNumber();
    const lastEventId = sseClient?.getLastEventId();
    
    console.log('Connecting with checkpoint:', { lastSequence, lastEventId });
    setMissedEventCount(0);
    
    baseConnect();
  }, [baseConnect, sseClient]);

  // Clear checkpoint and reconnect
  const clearCheckpointAndReconnect = useCallback(() => {
    if (sseClient) {
      sseClient.clearCheckpoint();
      setCheckpointInfo({ sequence: null, eventId: null });
      setEventLog([]);
      disconnect();
      setTimeout(() => connect(), 500);
    }
  }, [sseClient, disconnect, connect]);

  // Simulate disconnection with missed events
  const simulateDisconnectionWithMissedEvents = useCallback(() => {
    const currentSequence = checkpointInfo.sequence;
    disconnect();
    
    // Simulate that we missed some events
    setTimeout(() => {
      if (currentSequence !== null) {
        const estimatedMissed = Math.floor(Math.random() * 5) + 2; // 2-6 missed events
        setMissedEventCount(estimatedMissed);
      }
      connect();
    }, 3000);
  }, [disconnect, connect, checkpointInfo.sequence]);

  // Get checkpoint status display
  const getCheckpointStatus = () => {
    const lastSequence = sseClient?.getLastSequenceNumber();
    const lastEventId = sseClient?.getLastEventId();
    
    if (!useCheckpoint) {
      return 'Checkpoint disabled';
    }
    
    if (lastSequence !== null) {
      return `Last sequence: ${lastSequence}`;
    } else if (lastEventId) {
      return `Last event ID: ${lastEventId}`;
    } else {
      return 'No checkpoint saved';
    }
  };

  return (
    <div className="example-container">
      <h2>Checkpoint Recovery Example</h2>
      <div className="example-purpose" style={{ 
        backgroundColor: '#e8f5e9', 
        padding: '1rem', 
        borderRadius: '8px', 
        marginBottom: '1rem',
        border: '1px solid #66bb6a'
      }}>
        <h3>🎯 Purpose: Event Recovery on Reconnection</h3>
        <p>
          <strong>What this demonstrates:</strong> How checkpoint recovery ensures no events are lost during 
          disconnections by resuming from the last received sequence number.
        </p>
        <p>
          <strong>Key features:</strong> Automatic checkpoint saving, sequence tracking, event replay on reconnection
        </p>
        <p>
          <strong>Use this when:</strong> Building apps where event loss is unacceptable (financial data, notifications, etc.)
        </p>
      </div>

      <div className="checkpoint-status-panel">
        <h3>Checkpoint Status</h3>
        <div className="status-info">
          <p><strong>Connection:</strong> <span className={`status-${status === SseConnectionStatus.OPEN ? 'success' : 'error'}`}>{status}</span></p>
          <p><strong>Checkpoint:</strong> {getCheckpointStatus()}</p>
          <p><strong>Current Sequence:</strong> {checkpointInfo.sequence || 'N/A'}</p>
          <p><strong>Events Received:</strong> {eventLog.length}</p>
          {missedEventCount > 0 && (
            <p className="missed-events-info" style={{ color: '#ff6b6b' }}>
              <strong>⚠️ Estimated Missed Events:</strong> {missedEventCount} (will be recovered)
            </p>
          )}
        </div>
      </div>

      <div className="controls">
        <div className="form-group">
          <label>
            <input
              type="checkbox"
              checked={useCheckpoint}
              onChange={(e) => setUseCheckpoint(e.target.checked)}
              disabled={status === SseConnectionStatus.OPEN}
            />
            Use Checkpoint Recovery
          </label>
        </div>
        
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
        <button
          onClick={simulateDisconnectionWithMissedEvents}
          disabled={status !== SseConnectionStatus.OPEN}
        >
          Simulate Disconnection (3s)
        </button>
        <button
          onClick={clearCheckpointAndReconnect}
          disabled={status === SseConnectionStatus.OPEN}
        >
          Clear Checkpoint & Reconnect
        </button>
        <button onClick={() => {
          setEventLog([]);
          clearEvents();
        }}>
          Clear Event Log
        </button>
      </div>

      <div className="event-timeline">
        <h3>Event Timeline</h3>
        <div className="timeline-container" style={{ maxHeight: '400px', overflowY: 'auto' }}>
          {eventLog.length === 0 ? (
            <p className="no-events">No events received yet.</p>
          ) : (
            <div className="events-list">
              {eventLog.map((event, index) => (
                <div 
                  key={`${event.id}-${index}`} 
                  className={`event-item ${event.recovered ? 'recovered-event' : ''}`}
                  style={{
                    padding: '0.5rem',
                    margin: '0.25rem 0',
                    borderLeft: event.recovered ? '4px solid #ff9800' : '4px solid #4caf50',
                    backgroundColor: event.recovered ? '#fff3e0' : '#f5f5f5'
                  }}
                >
                  <div className="event-header">
                    <span className="event-sequence">
                      Seq: {event.sequenceNumber || 'N/A'}
                    </span>
                    <span className="event-timestamp">
                      {new Date(event.timestamp).toLocaleTimeString()}
                    </span>
                    {event.recovered && <span className="recovered-badge">RECOVERED</span>}
                  </div>
                  <div className="event-data">
                    {JSON.stringify(event.data, null, 2)}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      <div className="code-example">
        <h3>Code Example</h3>
        <pre>
{`import { useSse } from '../hooks/useSse.js';

function CheckpointRecoveryComponent() {
  const { status, events, sseClient } = useSse({
    url: '/api/sse/connect',
    clientId: 'my-client-id',
    useCheckpoint: true,              // Enable checkpoint recovery
    checkpointStorageKey: 'my-app',   // Custom storage key
    onMessage: (event) => {
      const data = JSON.parse(event.data);
      const sequence = data._sequence;
      console.log('Received event with sequence:', sequence);
    }
  });

  // Get checkpoint info
  const lastSequence = sseClient?.getLastSequenceNumber();
  const lastEventId = sseClient?.getLastEventId();

  // Clear checkpoint if needed
  const clearCheckpoint = () => {
    sseClient?.clearCheckpoint();
  };

  return (
    <div>
      <p>Last Sequence: {lastSequence || 'None'}</p>
      <p>Status: {status}</p>
      <button onClick={clearCheckpoint}>Clear Checkpoint</button>
    </div>
  );
}`}
        </pre>
      </div>
    </div>
  );
}