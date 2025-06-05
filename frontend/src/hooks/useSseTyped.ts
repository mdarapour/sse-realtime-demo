import { useEffect, useState, useRef, useCallback } from 'react';
import { SseClient, SseConnectionStatus } from '../services/sseService';
import type { SseOptions, SseEvent } from '../services/sseService';
import { SseEventTypes } from '../models/sseEventTypes';
import {
  EventPayload,
  NotificationPayload,
  DataUpdatePayload,
  HeartbeatPayload,
  isNotification,
  isDataUpdate,
  isHeartbeat
} from '../models/sseMessages';
import {
  parseEventPayload,
  parseNotification,
  parseDataUpdate,
  parseHeartbeat
} from '../models/sseSchemas';

/**
 * Hook options for useSseTyped
 */
export interface UseSseTypedOptions extends Omit<SseOptions, 'onOpen' | 'onClose' | 'onError' | 'onMessage' | 'onEvent'> {
  /** Whether to connect immediately (default: true) */
  autoConnect?: boolean;

  /** Callback for when a notification is received */
  onNotification?: (notification: NotificationPayload) => void;

  /** Callback for when a data update is received */
  onDataUpdate?: (dataUpdate: DataUpdatePayload) => void;

  /** Callback for when a heartbeat is received */
  onHeartbeat?: (heartbeat: HeartbeatPayload) => void;

  /** Callback for when any event is received */
  onAnyEvent?: (event: EventPayload) => void;
}

/**
 * Hook return type for useSseTyped
 */
export interface UseSseTypedResult {
  /** Current connection status */
  status: string;

  /** All events received */
  events: SseEvent[];

  /** Typed events received */
  typedEvents: EventPayload[];

  /** Notifications received */
  notifications: NotificationPayload[];

  /** Data updates received */
  dataUpdates: DataUpdatePayload[];

  /** Connect to the SSE endpoint */
  connect: () => void;

  /** Disconnect from the SSE endpoint */
  disconnect: () => void;

  /** Clear all events */
  clearEvents: () => void;
}

/**
 * Custom hook for using SSE with typed events
 * @param options Hook options
 * @returns Hook result
 */
export function useSseTyped(options: UseSseTypedOptions): UseSseTypedResult {
  const [status, setStatus] = useState<string>(SseConnectionStatus.CLOSED);
  const [events, setEvents] = useState<SseEvent[]>([]);
  const [typedEvents, setTypedEvents] = useState<EventPayload[]>([]);
  const [notifications, setNotifications] = useState<NotificationPayload[]>([]);
  const [dataUpdates, setDataUpdates] = useState<DataUpdatePayload[]>([]);

  const clientRef = useRef<SseClient | null>(null);

  // Initialize the SSE client
  useEffect(() => {
    if (import.meta.env.DEV) {
      console.debug('Initializing SSE client with options:', {
        url: options.url,
        clientId: options.clientId,
        filter: options.filter
      });
    }

    const sseOptions: SseOptions = {
      ...options,
      onOpen: () => {
        setStatus(SseConnectionStatus.OPEN);
      },
      onClose: () => {
        setStatus(SseConnectionStatus.CLOSED);
      },
      onError: (error) => {
        console.error('SSE connection error:', error);
        setStatus(SseConnectionStatus.ERROR);
      },
      onMessage: (event) => {
        setEvents((prevEvents) => [...prevEvents, event]);

        try {
          // Try to parse the event data as a typed event
          const payload = parseEventPayload(event.data);

          // Add to the typed events list
          setTypedEvents((prev) => [...prev, payload]);

          // Call the appropriate callback based on event type
          if (isNotification(payload) && options.onNotification) {
            options.onNotification(payload);
            setNotifications((prev) => [...prev, payload]);
          } else if (isDataUpdate(payload) && options.onDataUpdate) {
            options.onDataUpdate(payload);
            setDataUpdates((prev) => [...prev, payload]);
          } else if (isHeartbeat(payload) && options.onHeartbeat) {
            options.onHeartbeat(payload);
          }

          // Call the any event callback if provided
          if (options.onAnyEvent) {
            options.onAnyEvent(payload);
          }
        } catch (error) {
          console.error('Error parsing SSE event data:', error);
          console.error('Raw event data:', event.data);
        }
      },
      onEvent: {
        [SseEventTypes.Notification]: (data) => {
          try {
            const notification = parseNotification(JSON.stringify(data));
            if (options.onNotification) {
              options.onNotification(notification);
            }
            setNotifications((prev) => [...prev, notification]);
          } catch (error) {
            console.error('Error parsing notification event:', error);
          }
        },
        [SseEventTypes.DataUpdate]: (data) => {
          try {
            const dataUpdate = parseDataUpdate(JSON.stringify(data));
            if (options.onDataUpdate) {
              options.onDataUpdate(dataUpdate);
            }
            setDataUpdates((prev) => [...prev, dataUpdate]);
          } catch (error) {
            console.error('Error parsing data update event:', error);
          }
        },
        [SseEventTypes.Heartbeat]: (data) => {
          try {
            const heartbeat = parseHeartbeat(JSON.stringify(data));
            if (options.onHeartbeat) {
              options.onHeartbeat(heartbeat);
            }
          } catch (error) {
            console.error('Error parsing heartbeat event:', error);
          }
        }
      }
    };

    // Create a new SSE client
    clientRef.current = new SseClient(sseOptions);

    // Connect automatically if autoConnect is true
    if (options.autoConnect !== false) {
      clientRef.current.connect();
    }

    // Cleanup on unmount or when dependencies change
    return () => {
      if (clientRef.current) {
        clientRef.current.close();
        clientRef.current = null;
      }
    };
  }, [options.url, options.clientId, options.filter]);

  // Connect to the SSE endpoint
  const connect = useCallback(() => {
    if (clientRef.current) {
      clientRef.current.connect();
    } else {
      console.warn('Cannot connect: SSE client not initialized');
    }
  }, []);

  // Disconnect from the SSE endpoint
  const disconnect = useCallback(() => {
    if (clientRef.current) {
      clientRef.current.close();
    } else {
      console.warn('Cannot disconnect: SSE client not initialized');
    }
  }, []);

  // Clear all events
  const clearEvents = useCallback(() => {
    setEvents([]);
    setTypedEvents([]);
    setNotifications([]);
    setDataUpdates([]);
  }, []);

  return {
    status,
    events,
    typedEvents,
    notifications,
    dataUpdates,
    connect,
    disconnect,
    clearEvents
  };
}
