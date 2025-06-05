import { useEffect, useState, useRef, useCallback } from 'react';
import { SseClient, SseConnectionStatus } from '../services/sseService';
import type { SseOptions, SseEvent } from '../services/sseService';

/**
 * Hook options for useSse
 */
export interface UseSseOptions extends Omit<SseOptions, 'onOpen' | 'onClose' | 'onError' | 'onMessage'> {
  /** Whether to connect immediately (default: true) */
  autoConnect?: boolean;
  /** Maximum number of events to store (default: 100) */
  maxEvents?: number;
  /** Whether to auto-clear old events when max is reached (default: true) */
  autoClearOldEvents?: boolean;
}

/**
 * Hook return type for useSse
 */
export interface UseSseReturn {
  /** Current connection status */
  status: SseConnectionStatus;

  /** Last received event */
  lastEvent: SseEvent | null;

  /** All received events */
  events: SseEvent[];

  /** Connect to the SSE endpoint */
  connect: () => void;

  /** Disconnect from the SSE endpoint */
  disconnect: () => void;

  /** Clear the events array */
  clearEvents: () => void;
}

/**
 * Custom hook for using Server-Sent Events in React components
 * @param options SSE connection options
 * @returns Hook state and methods
 */
export function useSse(options: UseSseOptions): UseSseReturn {
  const [status, setStatus] = useState<SseConnectionStatus>(SseConnectionStatus.CLOSED);
  const [lastEvent, setLastEvent] = useState<SseEvent | null>(null);
  const [events, setEvents] = useState<SseEvent[]>([]);
  const clientRef = useRef<SseClient | null>(null);

  // Track processed event IDs to avoid duplicates
  const processedEventIds = useRef<Set<string>>(new Set());

  // Maximum number of event IDs to track (to prevent memory leaks)
  const MAX_TRACKED_EVENT_IDS = 1000;
  
  // Maximum number of events to store
  const maxEvents = options.maxEvents ?? 100;
  const autoClearOldEvents = options.autoClearOldEvents ?? true;

  // Create the SSE client
  useEffect(() => {
    const sseOptions: SseOptions = {
      ...options,
      onOpen: () => setStatus(SseConnectionStatus.OPEN),
      onClose: () => setStatus(SseConnectionStatus.CLOSED),
      onError: () => setStatus(SseConnectionStatus.ERROR),
      onMessage: (event) => {
        // Try to extract messageId from the event data
        let messageId = null;
        try {
          const data = JSON.parse(event.data);
          messageId = data.messageId;
        } catch (e) {
          // Not JSON or no messageId, continue without ID tracking
        }

        // Check if we've already processed this event
        if (messageId && processedEventIds.current.has(messageId)) {
          console.log(`Skipping duplicate event with ID: ${messageId}`);
          return;
        }

        // Update the last event
        setLastEvent(event);

        // Add to events array with automatic cleanup
        setEvents(prev => {
          const newEvents = [...prev, event];
          
          // If we've exceeded the max events, remove old ones
          if (autoClearOldEvents && newEvents.length > maxEvents) {
            // Keep only the most recent events
            const eventsToKeep = newEvents.slice(-maxEvents);
            
            // Update processed event IDs to match
            const keptEventIds = new Set<string>();
            eventsToKeep.forEach(evt => {
              try {
                const data = JSON.parse(evt.data);
                if (data.messageId) {
                  keptEventIds.add(data.messageId);
                }
              } catch (e) {
                // Not JSON or no messageId
              }
            });
            
            // Update the processed IDs to only include kept events
            processedEventIds.current = new Set(
              Array.from(processedEventIds.current).filter(id => keptEventIds.has(id))
            );
            
            return eventsToKeep;
          }
          
          return newEvents;
        });

        // Track this event ID to avoid duplicates
        if (messageId) {
          processedEventIds.current.add(messageId);

          // Clean up old event IDs if we're tracking too many
          if (processedEventIds.current.size > MAX_TRACKED_EVENT_IDS) {
            // Convert to array, remove oldest 20% of IDs, and convert back to Set
            const idsArray = Array.from(processedEventIds.current);
            const itemsToRemove = Math.floor(MAX_TRACKED_EVENT_IDS * 0.2);
            const newIds = new Set(idsArray.slice(itemsToRemove));
            processedEventIds.current = newIds;
          }
        }
      },
    };

    clientRef.current = new SseClient(sseOptions);

    // Connect immediately if autoConnect is true
    if (options.autoConnect !== false) {
      clientRef.current.connect();
    }

    // Clean up on unmount
    return () => {
      if (clientRef.current) {
        clientRef.current.close();
      }
    };
  }, [options.url, options.clientId, options.filter]); // Re-create client when these options change

  // Connect method
  const connect = useCallback(() => {
    if (clientRef.current) {
      clientRef.current.connect();
    }
  }, []);

  // Disconnect method
  const disconnect = useCallback(() => {
    if (clientRef.current) {
      clientRef.current.close();
    }
  }, []);

  // Clear events method
  const clearEvents = useCallback(() => {
    setEvents([]);
    setLastEvent(null);

    // Clear the processed event IDs
    processedEventIds.current.clear();
  }, []);

  return {
    status,
    lastEvent,
    events,
    connect,
    disconnect,
    clearEvents,
  };
}

/**
 * Custom hook for using Server-Sent Events with specific event types
 * @param options SSE connection options
 * @param eventTypes Event types to listen for
 * @returns Hook state and methods
 */
export function useSseEvents<T extends Record<string, any>>(
  options: UseSseOptions,
  eventTypes: string[]
): UseSseReturn & Record<string, T[]> {
  // Track processed event IDs to avoid duplicates
  const processedEventIds = useRef<Set<string>>(new Set());

  // Maximum number of event IDs to track (to prevent memory leaks)
  const MAX_TRACKED_EVENT_IDS = 1000;
  
  // Maximum number of events to store per type
  const maxEventsPerType = options.maxEvents ?? 100;
  const autoClearOldEvents = options.autoClearOldEvents ?? true;

  const [eventData, setEventData] = useState<Record<string, T[]>>(() => {
    const data: Record<string, T[]> = {};
    eventTypes.forEach(type => {
      // Use the event type as the key
      data[type] = [];
    });
    return data;
  });

  // Create event handlers for each event type
  const onEvent: Record<string, (data: any) => void> = {};

  // Add a handler for all message types
  onEvent['message'] = (data: any) => {
    try {
      // Try to extract type from the data
      const parsedData = typeof data === 'string' ? JSON.parse(data) : data;

      // Process events with a type property
      if (parsedData && parsedData.type && parsedData.type !== 'message') { // Skip 'message' type to avoid duplicates
        // Only process this event if we're interested in this event type
        // and there's no specific handler for it
        if (!eventTypes.includes(parsedData.type)) {
          return;
        }

        // Check if we've already processed this event
        if (parsedData.messageId && processedEventIds.current.has(parsedData.messageId)) {
          return;
        }

        // Process it as a typed event
        setEventData(prev => {
          let typeEvents = [...(prev[parsedData.type] || []), parsedData];
          
          // Apply max events limit per type
          if (autoClearOldEvents && typeEvents.length > maxEventsPerType) {
            typeEvents = typeEvents.slice(-maxEventsPerType);
          }
          
          const newData = {
            ...prev,
            [parsedData.type]: typeEvents,
          };

          // Track this event ID to avoid duplicates
          if (parsedData.messageId) {
            processedEventIds.current.add(parsedData.messageId);

            // Clean up old event IDs if we're tracking too many
            if (processedEventIds.current.size > MAX_TRACKED_EVENT_IDS) {
              // Convert to array, remove oldest 20% of IDs, and convert back to Set
              const idsArray = Array.from(processedEventIds.current);
              const itemsToRemove = Math.floor(MAX_TRACKED_EVENT_IDS * 0.2);
              const newIds = new Set(idsArray.slice(itemsToRemove));
              processedEventIds.current = newIds;
            }
          }

          return newData;
        });
      }
    } catch (error) {
      console.error('Error processing message event:', error);
    }
  };

  // Add handlers for specific event types
  eventTypes.forEach(type => {
    // Skip 'message' type as it's handled separately
    if (type === 'message') return;
    
    onEvent[type] = (data: T) => {
      // Check if we've already processed this event
      const eventId = (data as any).messageId;
      if (eventId && processedEventIds.current.has(eventId)) {
        return;
      }

      setEventData(prev => {
        let typeEvents = [...(prev[type] || []), data];
        
        // Apply max events limit per type
        if (autoClearOldEvents && typeEvents.length > maxEventsPerType) {
          typeEvents = typeEvents.slice(-maxEventsPerType);
        }
        
        const newData = {
          ...prev,
          [type]: typeEvents,
        };

        // Track this event ID to avoid duplicates
        if (eventId) {
          processedEventIds.current.add(eventId);

          // Clean up old event IDs if we're tracking too many
          if (processedEventIds.current.size > MAX_TRACKED_EVENT_IDS) {
            // Convert to array, remove oldest 20% of IDs, and convert back to Set
            const idsArray = Array.from(processedEventIds.current);
            const itemsToRemove = Math.floor(MAX_TRACKED_EVENT_IDS * 0.2);
            const newIds = new Set(idsArray.slice(itemsToRemove));
            processedEventIds.current = newIds;
          }
        }

        return newData;
      });
    };
  });

  // Use the base SSE hook
  const sseHook = useSse({
    ...options,
    onEvent,
  });

  // Clear events method that also clears typed event data
  const clearEvents = useCallback(() => {
    sseHook.clearEvents();

    // Clear all event data
    setEventData(prev => {
      const newData: Record<string, T[]> = { ...prev };
      Object.keys(newData).forEach(key => {
        newData[key] = [];
      });
      return newData;
    });

    // Clear the processed event IDs
    processedEventIds.current.clear();
  }, [sseHook.clearEvents]);

  // Create the return object with the correct type
  const result = {
    ...sseHook,
    ...eventData,
    clearEvents,
  };

  return result as UseSseReturn & Record<string, T[]>;
}
