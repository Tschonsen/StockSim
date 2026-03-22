import { createLogger } from './logger';

const log = createLogger('WebSocket');

export enum ConnectionState {
  Disconnected = 'disconnected',
  Connecting = 'connecting',
  Connected = 'connected',
}

type MessageHandler = (payload: unknown) => void;
type WebSocketFactory = (url: string) => WebSocket;

/**
 * WebSocket client for communication with the C# backend.
 * Handles connection lifecycle, message routing, and reconnection.
 * See Bible section 21.3 for the full protocol specification.
 */
export class WebSocketClient {
  private url: string;
  private ws: WebSocket | null = null;
  private wsFactory: WebSocketFactory;
  private handlers = new Map<string, Set<MessageHandler>>();
  private _state: ConnectionState = ConnectionState.Disconnected;
  private pingInterval: ReturnType<typeof setInterval> | null = null;

  get state(): ConnectionState {
    return this._state;
  }

  constructor(
    url: string = 'ws://localhost:8765',
    wsFactory?: WebSocketFactory
  ) {
    this.url = url;
    this.wsFactory = wsFactory ?? ((u: string) => new WebSocket(u));
  }

  connect(): void {
    if (this._state !== ConnectionState.Disconnected) {
      log.warn('Already connecting or connected');
      return;
    }

    log.info('Connecting to backend', { url: this.url });
    this._state = ConnectionState.Connecting;

    this.ws = this.wsFactory(this.url);

    this.ws.onopen = () => {
      this._state = ConnectionState.Connected;
      log.info('Connected to backend');
      this.sendHandshake();
      this.startPingInterval();
    };

    this.ws.onmessage = (event: MessageEvent) => {
      this.handleMessage(event.data as string);
    };

    this.ws.onclose = () => {
      this._state = ConnectionState.Disconnected;
      this.stopPingInterval();
      log.info('Disconnected from backend');
    };

    this.ws.onerror = () => {
      log.error('WebSocket error');
    };
  }

  disconnect(): void {
    this.stopPingInterval();
    this.ws?.close();
    this.ws = null;
    this._state = ConnectionState.Disconnected;
    log.info('Disconnected (manual)');
  }

  send(type: string, payload: unknown = {}): void {
    if (this._state !== ConnectionState.Connected || !this.ws) {
      log.warn('Cannot send — not connected', { type });
      return;
    }

    const message = JSON.stringify({ type, payload });
    this.ws.send(message);
    log.debug('Message sent', { type });
  }

  on(type: string, handler: MessageHandler): () => void {
    if (!this.handlers.has(type)) {
      this.handlers.set(type, new Set());
    }
    this.handlers.get(type)!.add(handler);

    // Return unsubscribe function
    return () => {
      this.handlers.get(type)?.delete(handler);
    };
  }

  private handleMessage(raw: string): void {
    try {
      const msg = JSON.parse(raw) as { type: string; payload?: unknown };
      const { type, payload } = msg;

      log.debug('Message received', { type });

      // Internal handling for pong (heartbeat)
      if (type === 'pong') {
        return;
      }

      // Dispatch to registered handlers
      const typeHandlers = this.handlers.get(type);
      if (typeHandlers) {
        for (const handler of typeHandlers) {
          handler(payload);
        }
      }
    } catch (err) {
      log.error('Failed to parse message', { raw, error: String(err) });
    }
  }

  private sendHandshake(): void {
    this.send('hello', { version: '0.1.0' });
    log.info('Handshake sent');
  }

  private startPingInterval(): void {
    this.pingInterval = setInterval(() => {
      this.send('ping', { timestamp: new Date().toISOString() });
    }, 5000);
  }

  private stopPingInterval(): void {
    if (this.pingInterval) {
      clearInterval(this.pingInterval);
      this.pingInterval = null;
    }
  }
}
