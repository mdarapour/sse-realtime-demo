import { SseEventTypes } from './sseEventTypes';

/**
 * Base interface for all SSE message payloads
 * This matches the C# BaseEventPayload class in the backend
 */
export interface BaseEventPayload {
  /** Unique identifier for the message */
  messageId: string;

  /** Timestamp when the message was created (UTC) */
  timestamp: string | Date;

  /** Schema version for backward compatibility */
  version: string;

  /** Message type discriminator */
  type: string;
}

/**
 * Notification message payload
 * This matches the C# NotificationPayload class in the backend
 */
export interface NotificationPayload extends BaseEventPayload {
  /** Type discriminator for notifications */
  type: typeof SseEventTypes.Notification;

  /** The notification message text */
  message: string;

  /** Severity level of the notification */
  severity: string;
}

/**
 * Data update message payload
 * This matches the C# DataUpdatePayload class in the backend
 */
export interface DataUpdatePayload extends BaseEventPayload {
  /** Type discriminator for data updates */
  type: typeof SseEventTypes.DataUpdate;

  /** ID of the entity that was updated */
  entityId: string;

  /** Type of the entity that was updated */
  entityType: string;

  /** Changes made to the entity */
  changes: Record<string, any>;
}

/**
 * Heartbeat message payload to keep the connection alive
 * This matches the C# HeartbeatPayload class in the backend
 */
export interface HeartbeatPayload extends BaseEventPayload {
  /** Type discriminator for heartbeats */
  type: typeof SseEventTypes.Heartbeat;
}

/**
 * Union type for all possible event payloads
 */
export type EventPayload =
  | NotificationPayload
  | DataUpdatePayload
  | HeartbeatPayload;

/**
 * Message type constants
 * @deprecated Use SseEventTypes from sseEventTypes.ts instead for consistent event type naming
 */
export const MessageTypes = {
  // Re-export SseEventTypes values for backward compatibility
  NOTIFICATION: SseEventTypes.Notification,
  DATA_UPDATE: SseEventTypes.DataUpdate,
  HEARTBEAT: SseEventTypes.Heartbeat,
} as const;

/**
 * Type guard to check if a payload is a notification
 */
export function isNotification(payload: BaseEventPayload): payload is NotificationPayload {
  return payload.type === SseEventTypes.Notification;
}

/**
 * Type guard to check if a payload is a data update
 */
export function isDataUpdate(payload: BaseEventPayload): payload is DataUpdatePayload {
  return payload.type === SseEventTypes.DataUpdate;
}

/**
 * Type guard to check if a payload is a heartbeat
 */
export function isHeartbeat(payload: BaseEventPayload): payload is HeartbeatPayload {
  return payload.type === SseEventTypes.Heartbeat;
}
