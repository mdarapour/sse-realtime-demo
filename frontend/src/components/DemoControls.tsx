import { useState, useEffect } from 'react';

interface DemoControlsProps {
  apiKey?: string;
}

export default function DemoControls({ apiKey }: DemoControlsProps) {
  const [isRunning, setIsRunning] = useState(false);
  const [intervalSeconds, setIntervalSeconds] = useState(5);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Check demo status on mount
  useEffect(() => {
    checkDemoStatus();
  }, []);

  const checkDemoStatus = async () => {
    try {
      const headers: HeadersInit = {};
      if (apiKey) {
        headers['X-API-Key'] = apiKey;
      }

      const response = await fetch('/api/demo/status', {
        headers
      });

      if (response.ok) {
        const data = await response.json();
        setIsRunning(data.isRunning);
      }
    } catch (err) {
      console.error('Error checking demo status:', err);
    }
  };

  const startDemo = async () => {
    setLoading(true);
    setError(null);

    try {
      const headers: HeadersInit = {
        'Content-Type': 'application/json',
      };
      if (apiKey) {
        headers['X-API-Key'] = apiKey;
      }

      const response = await fetch('/api/demo/start', {
        method: 'POST',
        headers,
        body: JSON.stringify({ intervalSeconds }),
      });

      if (!response.ok) {
        const errorData = await response.text();
        throw new Error(errorData || `HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      setIsRunning(true);
      console.log('Demo started:', data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to start demo');
      console.error('Error starting demo:', err);
    } finally {
      setLoading(false);
    }
  };

  const stopDemo = async () => {
    setLoading(true);
    setError(null);

    try {
      const headers: HeadersInit = {};
      if (apiKey) {
        headers['X-API-Key'] = apiKey;
      }

      const response = await fetch('/api/demo/stop', {
        method: 'POST',
        headers,
      });

      if (!response.ok) {
        const errorData = await response.text();
        throw new Error(errorData || `HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      setIsRunning(false);
      console.log('Demo stopped:', data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to stop demo');
      console.error('Error stopping demo:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{
      background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
      color: 'white',
      padding: '1.5rem',
      borderRadius: '8px',
      marginBottom: '2rem',
      boxShadow: '0 4px 6px rgba(0, 0, 0, 0.1)'
    }}>
      <h3 style={{ margin: '0 0 1rem 0', fontSize: '1.25rem' }}>
        🎭 Demo Mode Controls
      </h3>
      
      <p style={{ marginBottom: '1rem', opacity: 0.9 }}>
        Generate automatic events to see SSE in action. Events will cycle through notifications, alerts, data updates, and heartbeats.
      </p>

      <div style={{ 
        display: 'flex', 
        gap: '1rem', 
        alignItems: 'center',
        flexWrap: 'wrap'
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <label htmlFor="interval" style={{ fontSize: '0.9rem' }}>
            Interval (seconds):
          </label>
          <input
            id="interval"
            type="number"
            min="1"
            max="60"
            value={intervalSeconds}
            onChange={(e) => setIntervalSeconds(parseInt(e.target.value) || 5)}
            disabled={isRunning || loading}
            style={{
              padding: '0.25rem 0.5rem',
              borderRadius: '4px',
              border: '1px solid rgba(255, 255, 255, 0.3)',
              background: 'rgba(255, 255, 255, 0.2)',
              color: 'white',
              width: '60px',
            }}
          />
        </div>

        <button
          onClick={isRunning ? stopDemo : startDemo}
          disabled={loading}
          style={{
            padding: '0.5rem 1.5rem',
            borderRadius: '4px',
            border: 'none',
            background: isRunning ? '#e74c3c' : '#2ecc71',
            color: 'white',
            fontWeight: 'bold',
            cursor: loading ? 'not-allowed' : 'pointer',
            opacity: loading ? 0.7 : 1,
            transition: 'all 0.2s',
          }}
        >
          {loading ? 'Loading...' : isRunning ? '⏹ Stop Demo' : '▶️ Start Demo'}
        </button>

        {isRunning && (
          <span style={{
            display: 'flex',
            alignItems: 'center',
            gap: '0.5rem',
            fontSize: '0.9rem',
            opacity: 0.9
          }}>
            <span style={{
              display: 'inline-block',
              width: '8px',
              height: '8px',
              borderRadius: '50%',
              background: '#2ecc71',
              animation: 'pulse 2s infinite'
            }} />
            Generating events every {intervalSeconds} seconds
          </span>
        )}
      </div>

      {error && (
        <div style={{
          marginTop: '1rem',
          padding: '0.5rem',
          background: 'rgba(231, 76, 60, 0.2)',
          borderRadius: '4px',
          fontSize: '0.9rem'
        }}>
          ❌ {error}
        </div>
      )}

      <style>{`
        @keyframes pulse {
          0% { opacity: 1; }
          50% { opacity: 0.5; }
          100% { opacity: 1; }
        }
      `}</style>
    </div>
  );
}