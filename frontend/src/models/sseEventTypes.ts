/**
 * Shared event types for Server-Sent Events
 * This module defines all valid event types that can be used in the application
 * Both frontend and backend should use these constants to ensure consistency
 */

/**
 * Event type constants
 * These match exactly with the backend SseEventTypes.cs values
 */
export const SseEventTypes = {
  /** Default message event type */
  Message: 'message',

  /** Notification event type for user notifications */
  Notification: 'notification',

  /** Data update event type for entity changes */
  DataUpdate: 'dataUpdate',

  /** Alert event type for important alerts */
  Alert: 'alert',

  /** Heartbeat event type to keep connections alive */
  Heartbeat: 'heartbeat',

  /** Connected event type sent when a client connects */
  Connected: 'connected'
} as const;

/**
 * Type representing all valid event types
 */
export type SseEventType = typeof SseEventTypes[keyof typeof SseEventTypes];

/**
 * Validates if the provided event type is a valid SSE event type
 * @param eventType The event type to validate
 * @returns True if the event type is valid, false otherwise
 */
export function isValidEventType(eventType: string): eventType is SseEventType {
  return Object.values(SseEventTypes).includes(eventType as any);
}

/**
 * Gets all valid event types
 * @returns Array of all valid event types
 */
export function getAllEventTypes(): SseEventType[] {
  return Object.values(SseEventTypes);
}

/**
 * Maps from filter name to event type
 * This is used to handle legacy filter names that don't match event types
 */
export const EventTypeFilterMap: Record<string, SseEventType> = {
  // Standard mappings (same name)
  [SseEventTypes.Message]: SseEventTypes.Message,
  [SseEventTypes.Notification]: SseEventTypes.Notification,
  [SseEventTypes.DataUpdate]: SseEventTypes.DataUpdate,
  [SseEventTypes.Alert]: SseEventTypes.Alert,
  [SseEventTypes.Heartbeat]: SseEventTypes.Heartbeat,
  [SseEventTypes.Connected]: SseEventTypes.Connected,

  // Legacy mappings (different names)
  'update': SseEventTypes.DataUpdate
};

/**
 * Gets the event type for a given filter
 * @param filter The filter name
 * @returns The corresponding event type or the original filter if not found
 */
export function getEventTypeForFilter(filter: string): string {
  return EventTypeFilterMap[filter] || filter;
}
