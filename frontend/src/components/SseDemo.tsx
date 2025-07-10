import { useState } from 'react';
import BasicExample from './examples/BasicExample';
import FilteredEventsExample from './examples/FilteredEventsExample';
import MultipleStreamsExample from './examples/MultipleStreamsExample';
import ReconnectionExample from './examples/ReconnectionExample';
import CustomEventsExample from './examples/CustomEventsExample';
import TypedEventsExample from './examples/TypedEventsExample';
import CheckpointRecoveryExample from './examples/CheckpointRecoveryExample';
import DemoControls from './DemoControls';

// Get configuration from environment variables or use defaults
// When VITE_API_URL is empty or not set, use relative paths (for Ingress/proxy setup)
// When VITE_API_URL is set, use it as the base URL (for direct access)
const BACKEND_URL = import.meta.env.VITE_API_URL 
  ? `${import.meta.env.VITE_API_URL}/api/sse/connect`
  : '/api/sse/connect';  // Default to relative path for Ingress

const API_KEY = import.meta.env.VITE_API_KEY || 'demo-api-key-12345';

/**
 * Main SSE Demo component
 */
export default function SseDemo() {
  const [activeTab, setActiveTab] = useState('basic');
  const [clientId] = useState(() => `client-${Math.random().toString(36).substring(2, 9)}`);
  const [showApiKeyWarning] = useState(!API_KEY);

  // Tabs configuration
  const tabs = [
    { id: 'basic', label: 'Basic Usage', component: BasicExample },
    { id: 'filtered', label: 'Filtered Events', component: FilteredEventsExample },
    { id: 'multiple', label: 'Multiple Streams', component: MultipleStreamsExample },
    { id: 'reconnection', label: 'Reconnection', component: ReconnectionExample },
    { id: 'checkpoint', label: 'Checkpoint Recovery', component: CheckpointRecoveryExample },
    { id: 'custom', label: 'Custom Events', component: CustomEventsExample },
    { id: 'typed', label: 'Typed Events', component: TypedEventsExample },
  ];

  return (
    <div className="sse-demo">
      <header>
        <h1>Server-Sent Events (SSE) Demo</h1>
        <p>
          This demo showcases various patterns and features of Server-Sent Events (SSE) using React and TypeScript
          on the frontend and C# on the backend.
        </p>
        <details style={{ marginTop: '1rem', marginBottom: '1rem' }}>
          <summary style={{ cursor: 'pointer', fontWeight: 'bold', color: '#1976d2' }}>
            📚 Example Overview (click to expand)
          </summary>
          <div style={{ marginTop: '0.5rem', padding: '1rem', backgroundColor: '#f5f5f5', borderRadius: '8px' }}>
            <ul style={{ listStyle: 'none', padding: 0 }}>
              <li>🔵 <strong>Basic:</strong> Simple connection and event receiving - perfect for getting started</li>
              <li>🟣 <strong>Filtered:</strong> Server-side filtering to reduce bandwidth usage</li>
              <li>🟢 <strong>Multiple:</strong> Manage multiple concurrent connections for complex apps</li>
              <li>🟠 <strong>Reconnection:</strong> Handle network failures with automatic recovery</li>
              <li>⚡ <strong>Checkpoint:</strong> Resume from last received event after disconnection - no event loss</li>
              <li>🔴 <strong>Custom:</strong> Send events between clients for collaborative features</li>
              <li>🔷 <strong>Typed:</strong> Type-safe events with validation for production apps</li>
            </ul>
          </div>
        </details>
        <p>
          <strong>Your Client ID:</strong> {clientId}
        </p>
      </header>

      <DemoControls apiKey={API_KEY} />

      <div className="tabs">
        <div className="tab-buttons">
          {tabs.map(tab => (
            <button
              key={tab.id}
              className={activeTab === tab.id ? 'active' : ''}
              onClick={() => setActiveTab(tab.id)}
            >
              {tab.label}
            </button>
          ))}
        </div>

        <div className="tab-content">
          {tabs.map(tab => (
            <div key={tab.id} className={`tab-pane ${activeTab === tab.id ? 'active' : ''}`}>
              {activeTab === tab.id && <tab.component clientId={clientId} backendUrl={BACKEND_URL} apiKey={API_KEY} />}
            </div>
          ))}
        </div>
      </div>

      <footer>
        <p>
          <strong>Note:</strong> Make sure the backend server is running on{' '}
          <code>http://localhost:5121</code> for this demo to work.
        </p>
        {showApiKeyWarning && (
          <p className="warning">
            <strong>Warning:</strong> No API key configured. SSE connections will fail.
          </p>
        )}
      </footer>
    </div>
  );
}
