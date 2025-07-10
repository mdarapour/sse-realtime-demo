import { SseEventTypes, isValidEventType } from '../models/sseEventTypes';

/**
 * SSE Event interface
 */
export interface SseEvent {
  id?: string;
  event?: string;
  data: string;
  retry?: number;
}

/**
 * SSE Connection options
 */
export interface SseOptions {
  /** URL for the SSE endpoint */
  url: string;

  /** Optional client ID */
  clientId?: string;

  /** Optional event filter */
  filter?: string;
  
  /** Optional API key for authentication */
  apiKey?: string;

  /** Retry timeout in milliseconds (default: 3000) */
  retryTimeout?: number;

  /** Maximum number of retry attempts (default: 5) */
  maxRetryAttempts?: number;

  /** Whether to automatically reconnect on error (default: true) */
  autoReconnect?: boolean;

  /** Whether to use checkpoint recovery on reconnection (default: true) */
  useCheckpoint?: boolean;

  /** Storage key prefix for checkpoint data (default: 'sse-checkpoint') */
  checkpointStorageKey?: string;

  /** Callback for when the connection is opened */
  onOpen?: () => void;

  /** Callback for when the connection is closed */
  onClose?: () => void;

  /** Callback for when an error occurs */
  onError?: (error: Event) => void;

  /** Callback for when a message is received */
  onMessage?: (event: SseEvent) => void;

  /** Callback for specific event types */
  onEvent?: Record<string, (data: any) => void>;
}

/**
 * SSE Connection status
 */
export const SseConnectionStatus = {
  CONNECTING: 'connecting',
  OPEN: 'open',
  CLOSED: 'closed',
  ERROR: 'error',
} as const;

export type SseConnectionStatus = typeof SseConnectionStatus[keyof typeof SseConnectionStatus];

/**
 * SSE Client Service
 * Provides a wrapper around the EventSource API with additional features
 */
export class SseClient {
  private eventSource: EventSource | null = null;
  private options: SseOptions;
  private status: string = SseConnectionStatus.CLOSED;
  private retryCount = 0;
  private retryTimer: number | null = null;
  private eventListeners: Map<string, ((event: MessageEvent) => void)[]> = new Map();
  private lastSequenceNumber: number | null = null;
  private lastEventId: string | null = null;

  /**
   * Creates a new SSE client
   * @param options SSE connection options
   */
  constructor(options: SseOptions) {
    this.options = {
      retryTimeout: 3000,
      maxRetryAttempts: 5,
      autoReconnect: true,
      useCheckpoint: true,
      checkpointStorageKey: 'sse-checkpoint',
      ...options,
    };

    // Load checkpoint from localStorage if available
    if (this.options.useCheckpoint) {
      this.loadCheckpoint();
    }
  }

  /**
   * Gets the current connection status
   */
  public getStatus(): string {
    return this.status;
  }

  /**
   * Gets the last sequence number received
   */
  public getLastSequenceNumber(): number | null {
    return this.lastSequenceNumber;
  }

  /**
   * Gets the last event ID received
   */
  public getLastEventId(): string | null {
    return this.lastEventId;
  }

  /**
   * Loads checkpoint data from localStorage
   */
  private loadCheckpoint(): void {
    try {
      const storageKey = this.getCheckpointStorageKey();
      const checkpointData = localStorage.getItem(storageKey);
      
      if (checkpointData) {
        const checkpoint = JSON.parse(checkpointData);
        this.lastSequenceNumber = checkpoint.sequenceNumber || null;
        this.lastEventId = checkpoint.eventId || null;
        console.log('Loaded checkpoint:', { sequenceNumber: this.lastSequenceNumber, eventId: this.lastEventId });
      }
    } catch (error) {
      console.error('Error loading checkpoint:', error);
    }
  }

  /**
   * Saves checkpoint data to localStorage
   */
  private saveCheckpoint(): void {
    if (!this.options.useCheckpoint) {
      return;
    }

    try {
      const storageKey = this.getCheckpointStorageKey();
      const checkpointData = {
        sequenceNumber: this.lastSequenceNumber,
        eventId: this.lastEventId,
        timestamp: new Date().toISOString(),
        clientId: this.options.clientId
      };
      
      localStorage.setItem(storageKey, JSON.stringify(checkpointData));
    } catch (error) {
      console.error('Error saving checkpoint:', error);
    }
  }

  /**
   * Clears checkpoint data from localStorage
   */
  public clearCheckpoint(): void {
    try {
      const storageKey = this.getCheckpointStorageKey();
      localStorage.removeItem(storageKey);
      this.lastSequenceNumber = null;
      this.lastEventId = null;
      console.log('Checkpoint cleared');
    } catch (error) {
      console.error('Error clearing checkpoint:', error);
    }
  }

  /**
   * Gets the storage key for checkpoint data
   */
  private getCheckpointStorageKey(): string {
    const baseKey = this.options.checkpointStorageKey || 'sse-checkpoint';
    return this.options.clientId ? `${baseKey}-${this.options.clientId}` : baseKey;
  }

  /**
   * Connects to the SSE endpoint
   */
  public connect(): void {
    if (this.eventSource) {
      this.close();
    }

    // Clear existing event listeners
    this.eventListeners.clear();

    this.status = SseConnectionStatus.CONNECTING;
    console.log('SSE connecting...');

    // Build the URL with query parameters
    let url = this.options.url;
    const params = new URLSearchParams();

    if (this.options.clientId) {
      params.append('clientId', this.options.clientId);
    }

    if (this.options.filter) {
      // Validate the filter is a known event type
      if (!isValidEventType(this.options.filter) && this.options.filter !== 'update') {
        console.warn(`Unknown event filter type: ${this.options.filter}`);
      }

      // Use the filter as-is (backend will handle normalization)
      params.append('filter', this.options.filter);
    }
    
    // Add API key to query params if provided (since headers don't work with EventSource)
    if (this.options.apiKey) {
      params.append('apikey', this.options.apiKey);
    }

    // Add checkpoint parameters if available and checkpoint is enabled
    if (this.options.useCheckpoint && this.lastSequenceNumber !== null) {
      params.append('checkpoint', this.lastSequenceNumber.toString());
      console.log('Adding checkpoint to URL:', this.lastSequenceNumber);
    } else if (this.options.useCheckpoint && this.lastEventId) {
      params.append('lastEventId', this.lastEventId);
      console.log('Adding lastEventId to URL:', this.lastEventId);
    }

    if (params.toString()) {
      url += `?${params.toString()}`;
    }

    console.log('Connecting to SSE endpoint:', url);

    try {
      this.eventSource = new EventSource(url);
      console.log('EventSource created');

      // Set up event handlers
      this.eventSource.onopen = this.handleOpen.bind(this);
      this.eventSource.onerror = this.handleError.bind(this);

      // Set up message handler (only use one method to avoid duplicates)
      this.eventSource.onmessage = this.handleMessage.bind(this);
      console.log('Added onmessage handler');

      // Set up handlers for specific event types if provided
      if (this.options.onEvent) {
        console.log('Setting up specific event handlers for:', Object.keys(this.options.onEvent));

        // First, set up a handler for the generic 'message' event to catch all events
        if (this.options.onEvent['message']) {
          console.log('Setting up generic message handler');
          this.setupEventListener('message', (event) => {
            console.log('Received generic message event:', event);
            
            // Update last event ID and extract sequence number
            if (event.lastEventId) {
              this.lastEventId = event.lastEventId;
            }
            this.extractAndUpdateSequenceNumber(event.data);
            
            try {
              const data = JSON.parse(event.data);
              console.log('Parsed message data:', data);
              this.options.onEvent!['message'](data);
            } catch (error) {
              console.error('Error parsing message data:', error);
              this.options.onEvent!['message'](event.data);
            }
          });
        }

        // Then set up handlers for specific event types
        Object.keys(this.options.onEvent).forEach(eventType => {
          if (eventType === 'message') return; // Skip, already handled above

          console.log(`Adding listener for event type: ${eventType}`);
          this.setupEventListener(eventType, (event) => {
            console.log(`Received ${eventType} event:`, event);
            
            // Update last event ID and extract sequence number
            if (event.lastEventId) {
              this.lastEventId = event.lastEventId;
            }
            this.extractAndUpdateSequenceNumber(event.data);
            
            if (this.options.onEvent && this.options.onEvent[eventType]) {
              try {
                const data = JSON.parse(event.data);
                console.log(`Parsed ${eventType} data:`, data);
                this.options.onEvent![eventType](data);
              } catch (error) {
                console.error(`Error parsing event data for ${eventType}:`, error);
                this.options.onEvent![eventType](event.data);
              }
            }
          });
        });
      }

      // Only add listeners for event types that don't have specific handlers
      console.log('Adding listeners for remaining event types');

      // Add listeners for event types that don't have specific handlers
      const handledEventTypes = this.options.onEvent ? Object.keys(this.options.onEvent) : [];
      const eventTypes = Object.values(SseEventTypes).filter(type => !handledEventTypes.includes(type));

      if (eventTypes.length > 0) {
        console.log('Adding listeners for these event types:', eventTypes);
        eventTypes.forEach(eventType => {
          this.setupEventListener(eventType, (event) => {
            console.log(`Received ${eventType} event:`, event);
            
            // Update last event ID and extract sequence number
            if (event.lastEventId) {
              this.lastEventId = event.lastEventId;
            }
            this.extractAndUpdateSequenceNumber(event.data);
            
            if (this.options.onMessage) {
              const sseEvent: SseEvent = {
                id: event.lastEventId,
                event: eventType,
                data: event.data,
              };
              this.options.onMessage(sseEvent);
            }
          });
        });
      }
    } catch (error) {
      this.status = SseConnectionStatus.ERROR;
      console.error('Error creating EventSource:', error);
      this.scheduleReconnect();
    }
  }

  /**
   * Sets up an event listener for a specific event type
   * This is a helper method to ensure event listeners are properly registered
   */
  private setupEventListener(eventType: string, callback: (event: MessageEvent) => void): void {
    // Add the callback to our internal event listeners map
    this.addEventListener(eventType, callback);

    // Directly add the event listener to the EventSource
    if (this.eventSource) {
      console.log(`Directly adding event listener for: ${eventType} to EventSource`);
      this.eventSource.addEventListener(eventType, callback);
    } else {
      console.error(`Cannot add event listener for ${eventType}: EventSource is null`);
    }
  }

  /**
   * Closes the SSE connection
   */
  public close(): void {
    console.log('Closing SSE connection');

    if (this.eventSource) {
      // Clean up event listeners before closing
      console.log('Cleaning up event listeners');

      // Clean up standard event listeners
      this.eventSource.onopen = null;
      this.eventSource.onerror = null;
      this.eventSource.onmessage = null;

      // Close the connection
      this.eventSource.close();
      this.eventSource = null;
    }

    this.status = SseConnectionStatus.CLOSED;

    if (this.retryTimer !== null) {
      window.clearTimeout(this.retryTimer);
      this.retryTimer = null;
    }

    if (this.options.onClose) {
      this.options.onClose();
    }

    console.log('SSE connection closed');
  }

  /**
   * Adds an event listener for a specific event type
   * @param eventType Event type to listen for
   * @param callback Callback function to call when the event is received
   */
  public addEventListener(eventType: string, callback: (event: MessageEvent) => void): void {
    if (!this.eventListeners.has(eventType)) {
      this.eventListeners.set(eventType, []);

      // Add the event listener to the EventSource if it exists
      if (this.eventSource) {
        console.log(`Adding event listener for: ${eventType} to EventSource`);
        this.eventSource.addEventListener(eventType, (event) => {
          console.log(`Event received for ${eventType}:`, event);
          const listeners = this.eventListeners.get(eventType) || [];
          console.log(`Calling ${listeners.length} listeners for ${eventType}`);
          listeners.forEach(listener => listener(event));
        });
      } else {
        console.error(`Cannot add event listener for ${eventType}: EventSource is null`);
      }
    }

    const listeners = this.eventListeners.get(eventType) || [];
    listeners.push(callback);
    this.eventListeners.set(eventType, listeners);
  }

  /**
   * Removes an event listener for a specific event type
   * @param eventType Event type to remove the listener for
   * @param callback Callback function to remove
   */
  public removeEventListener(eventType: string, callback: (event: MessageEvent) => void): void {
    if (this.eventListeners.has(eventType)) {
      const listeners = this.eventListeners.get(eventType) || [];
      const index = listeners.indexOf(callback);

      if (index !== -1) {
        listeners.splice(index, 1);
        this.eventListeners.set(eventType, listeners);
      }
    }
  }

  /**
   * Handles the open event
   */
  private handleOpen(): void {
    console.log('SSE connection opened');
    this.status = SseConnectionStatus.OPEN;
    this.retryCount = 0;

    if (this.options.onOpen) {
      this.options.onOpen();
    }
  }

  /**
   * Handles the error event
   */
  private handleError(event: Event): void {
    console.error('SSE connection error:', event);
    this.status = SseConnectionStatus.ERROR;

    if (this.options.onError) {
      this.options.onError(event);
    }

    this.scheduleReconnect();
  }

  /**
   * Handles the message event
   */
  private handleMessage(event: MessageEvent): void {
    console.log('SSE message received:', event);

    // Update last event ID
    if (event.lastEventId) {
      this.lastEventId = event.lastEventId;
    }

    // Try to extract sequence number from the data
    this.extractAndUpdateSequenceNumber(event.data);

    if (this.options.onMessage) {
      const sseEvent: SseEvent = {
        id: event.lastEventId,
        event: 'message',
        data: event.data,
      };

      console.log('Created SSE event object:', sseEvent);
      this.options.onMessage(sseEvent);
    } else {
      console.warn('No message handler defined');
    }
  }

  /**
   * Extracts sequence number from event data and updates checkpoint
   */
  private extractAndUpdateSequenceNumber(data: string): void {
    try {
      const parsedData = JSON.parse(data);
      if (parsedData._sequence !== undefined) {
        this.lastSequenceNumber = parsedData._sequence;
        console.log('Updated sequence number:', this.lastSequenceNumber);
        
        // Save checkpoint after updating sequence number
        this.saveCheckpoint();
      }
    } catch {
      // Data might not be JSON or might not contain sequence number
      // This is fine, not all events may have sequence numbers
    }
  }

  /**
   * Schedules a reconnection attempt
   */
  private scheduleReconnect(): void {
    if (!this.options.autoReconnect) {
      return;
    }

    if (this.retryTimer !== null) {
      window.clearTimeout(this.retryTimer);
      this.retryTimer = null;
    }

    if (this.retryCount < (this.options.maxRetryAttempts || 5)) {
      this.retryCount++;
      const delay = this.options.retryTimeout || 3000;

      console.log(`Scheduling reconnection attempt ${this.retryCount} in ${delay}ms`);

      this.retryTimer = window.setTimeout(() => {
        console.log(`Attempting to reconnect (${this.retryCount}/${this.options.maxRetryAttempts})`);
        this.connect();
      }, delay);
    } else {
      console.error(`Maximum retry attempts (${this.options.maxRetryAttempts}) reached`);
      this.close();
    }
  }
}
