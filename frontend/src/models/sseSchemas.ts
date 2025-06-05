import { z } from 'zod';

/**
 * Base schema for all SSE message payloads
 */
export const BaseEventSchema = z.object({
  messageId: z.string().uuid(),
  timestamp: z.string().datetime(),
  version: z.string(),
  type: z.string()
});

/**
 * Schema for notification messages
 */
export const NotificationSchema = BaseEventSchema.extend({
  type: z.literal('notification'),
  message: z.string(),
  severity: z.enum(['info', 'warning', 'error'])
});

/**
 * Schema for data update messages
 */
export const DataUpdateSchema = BaseEventSchema.extend({
  type: z.literal('dataUpdate'),
  entityId: z.string(),
  entityType: z.string(),
  changes: z.record(z.any())
});

/**
 * Schema for heartbeat messages
 */
export const HeartbeatSchema = BaseEventSchema.extend({
  type: z.literal('heartbeat')
});

/**
 * Union schema for all possible event payloads
 */
export const EventPayloadSchema = z.discriminatedUnion('type', [
  NotificationSchema,
  DataUpdateSchema,
  HeartbeatSchema
]);

/**
 * Parse and validate a message payload
 * @param data The raw JSON string from the SSE event
 * @returns The validated payload or throws an error
 */
export function parseEventPayload(data: string) {
  try {
    console.log('Parsing event payload:', data);
    const json = JSON.parse(data);
    console.log('Parsed JSON:', json);

    // Try to validate against our schema
    const result = EventPayloadSchema.safeParse(json);

    if (result.success) {
      console.log('Validation successful:', result.data);
      return result.data;
    } else {
      console.warn('Schema validation failed:', result.error);
      // If validation fails, return the raw JSON as a fallback
      return json;
    }
  } catch (error) {
    console.error('Invalid SSE message format:', error);
    console.error('Raw data:', data);
    throw error;
  }
}

/**
 * Parse and validate a notification payload
 * @param data The raw JSON string from the SSE event
 * @returns The validated notification payload or throws an error
 */
export function parseNotification(data: string) {
  try {
    console.log('Parsing notification payload:', data);
    const json = JSON.parse(data);
    console.log('Parsed notification JSON:', json);

    const result = NotificationSchema.safeParse(json);
    if (result.success) {
      return result.data;
    } else {
      console.warn('Notification schema validation failed:', result.error);
      return json;
    }
  } catch (error) {
    console.error('Invalid notification format:', error);
    throw error;
  }
}

/**
 * Parse and validate a data update payload
 * @param data The raw JSON string from the SSE event
 * @returns The validated data update payload or throws an error
 */
export function parseDataUpdate(data: string) {
  try {
    console.log('Parsing data update payload:', data);
    const json = JSON.parse(data);
    console.log('Parsed data update JSON:', json);

    const result = DataUpdateSchema.safeParse(json);
    if (result.success) {
      return result.data;
    } else {
      console.warn('Data update schema validation failed:', result.error);
      return json;
    }
  } catch (error) {
    console.error('Invalid data update format:', error);
    throw error;
  }
}

/**
 * Parse and validate a heartbeat payload
 * @param data The raw JSON string from the SSE event
 * @returns The validated heartbeat payload or throws an error
 */
export function parseHeartbeat(data: string) {
  try {
    console.log('Parsing heartbeat payload:', data);
    const json = JSON.parse(data);
    console.log('Parsed heartbeat JSON:', json);

    const result = HeartbeatSchema.safeParse(json);
    if (result.success) {
      return result.data;
    } else {
      console.warn('Heartbeat schema validation failed:', result.error);
      return json;
    }
  } catch (error) {
    console.error('Invalid heartbeat format:', error);
    throw error;
  }
}
