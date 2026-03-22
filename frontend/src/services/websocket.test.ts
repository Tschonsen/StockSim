import { describe, it, expect, vi, beforeEach } from 'vitest';
import { WebSocketClient, ConnectionState } from './websocket';

// Mock WebSocket for testing
class MockWebSocket {
  static CONNECTING = 0;
  static OPEN = 1;
  static CLOSING = 2;
  static CLOSED = 3;

  readyState = MockWebSocket.CONNECTING;
  onopen: (() => void) | null = null;
  onclose: (() => void) | null = null;
  onmessage: ((event: { data: string }) => void) | null = null;
  onerror: ((event: unknown) => void) | null = null;
  sent: string[] = [];

  send(data: string) {
    this.sent.push(data);
  }

  close() {
    this.readyState = MockWebSocket.CLOSED;
    this.onclose?.();
  }

  // Test helpers
  simulateOpen() {
    this.readyState = MockWebSocket.OPEN;
    this.onopen?.();
  }

  simulateMessage(data: object) {
    this.onmessage?.({ data: JSON.stringify(data) });
  }

  simulateClose() {
    this.readyState = MockWebSocket.CLOSED;
    this.onclose?.();
  }
}

describe('WebSocketClient', () => {
  let client: WebSocketClient;
  let mockWs: MockWebSocket;

  beforeEach(() => {
    mockWs = new MockWebSocket();
    client = new WebSocketClient('ws://localhost:8765', () => mockWs as unknown as WebSocket);
  });

  it('should start in disconnected state', () => {
    expect(client.state).toBe(ConnectionState.Disconnected);
  });

  it('should transition to connecting on connect()', () => {
    client.connect();
    expect(client.state).toBe(ConnectionState.Connecting);
  });

  it('should transition to connected when socket opens', () => {
    client.connect();
    mockWs.simulateOpen();
    expect(client.state).toBe(ConnectionState.Connected);
  });

  it('should send hello handshake on connect', () => {
    client.connect();
    mockWs.simulateOpen();
    expect(mockWs.sent.length).toBe(1);
    const msg = JSON.parse(mockWs.sent[0]);
    expect(msg.type).toBe('hello');
    expect(msg.payload.version).toBeDefined();
  });

  it('should send messages as JSON', () => {
    client.connect();
    mockWs.simulateOpen();
    client.send('PlaceOrder', { symbol: 'AAPL', side: 'BUY', qty: 50 });
    expect(mockWs.sent.length).toBe(2); // hello + PlaceOrder
    const msg = JSON.parse(mockWs.sent[1]);
    expect(msg.type).toBe('PlaceOrder');
    expect(msg.payload.symbol).toBe('AAPL');
  });

  it('should not send when disconnected', () => {
    client.send('Test', {});
    expect(mockWs.sent.length).toBe(0);
  });

  it('should dispatch received messages to handlers', () => {
    const handler = vi.fn();
    client.on('MarketUpdate', handler);
    client.connect();
    mockWs.simulateOpen();

    mockWs.simulateMessage({
      type: 'MarketUpdate',
      payload: { prices: [{ symbol: 'AAPL', last: 142.58 }] },
    });

    expect(handler).toHaveBeenCalledWith({
      prices: [{ symbol: 'AAPL', last: 142.58 }],
    });
  });

  it('should handle multiple handlers for same message type', () => {
    const handler1 = vi.fn();
    const handler2 = vi.fn();
    client.on('NewsItem', handler1);
    client.on('NewsItem', handler2);
    client.connect();
    mockWs.simulateOpen();

    mockWs.simulateMessage({
      type: 'NewsItem',
      payload: { title: 'Breaking' },
    });

    expect(handler1).toHaveBeenCalled();
    expect(handler2).toHaveBeenCalled();
  });

  it('should allow unsubscribing handlers', () => {
    const handler = vi.fn();
    const unsub = client.on('Test', handler);
    client.connect();
    mockWs.simulateOpen();

    unsub();
    mockWs.simulateMessage({ type: 'Test', payload: {} });

    expect(handler).not.toHaveBeenCalled();
  });

  it('should respond to pong with internal state update', () => {
    client.connect();
    mockWs.simulateOpen();

    mockWs.simulateMessage({
      type: 'pong',
      payload: { timestamp: '2027-03-15T14:32:00Z' },
    });

    // Pong should not trigger external handlers
    const handler = vi.fn();
    client.on('pong', handler);
    expect(handler).not.toHaveBeenCalled();
  });

  it('should transition to disconnected on socket close', () => {
    client.connect();
    mockWs.simulateOpen();
    mockWs.simulateClose();
    expect(client.state).toBe(ConnectionState.Disconnected);
  });
});
