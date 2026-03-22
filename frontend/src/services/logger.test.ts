import { describe, it, expect, vi, beforeEach } from 'vitest';
import { Logger, LogLevel } from './logger';

describe('Logger', () => {
  let logger: Logger;

  beforeEach(() => {
    logger = new Logger('TestModule');
    vi.spyOn(console, 'log').mockImplementation(() => {});
    vi.spyOn(console, 'warn').mockImplementation(() => {});
    vi.spyOn(console, 'error').mockImplementation(() => {});
  });

  it('should create logger with module name', () => {
    expect(logger.module).toBe('TestModule');
  });

  it('should log DEBUG messages with correct format', () => {
    logger.debug('test message');
    expect(console.log).toHaveBeenCalledWith(
      expect.stringContaining('[DEBUG]'),
      expect.stringContaining('[TestModule]'),
      'test message'
    );
  });

  it('should log INFO messages', () => {
    logger.info('server started');
    expect(console.log).toHaveBeenCalledWith(
      expect.stringContaining('[INFO]'),
      expect.stringContaining('[TestModule]'),
      'server started'
    );
  });

  it('should log WARN messages', () => {
    logger.warn('low memory');
    expect(console.warn).toHaveBeenCalledWith(
      expect.stringContaining('[WARN]'),
      expect.stringContaining('[TestModule]'),
      'low memory'
    );
  });

  it('should log ERROR messages with context', () => {
    logger.error('connection failed', { host: 'localhost', port: 8765 });
    expect(console.error).toHaveBeenCalledWith(
      expect.stringContaining('[ERROR]'),
      expect.stringContaining('[TestModule]'),
      'connection failed',
      { host: 'localhost', port: 8765 }
    );
  });

  it('should respect minimum log level', () => {
    logger.setLevel(LogLevel.WARN);
    logger.debug('should not appear');
    logger.info('should not appear');
    logger.warn('should appear');
    expect(console.log).not.toHaveBeenCalled();
    expect(console.warn).toHaveBeenCalledTimes(1);
  });

  it('should include timestamp in log output', () => {
    logger.info('timestamped');
    const call = (console.log as ReturnType<typeof vi.fn>).mock.calls[0];
    expect(call[0]).toMatch(/\d{2}:\d{2}:\d{2}\.\d{3}/);
  });

  it('should store log history', () => {
    logger.info('first');
    logger.warn('second');
    const history = logger.getHistory();
    expect(history).toHaveLength(2);
    expect(history[0].message).toBe('first');
    expect(history[0].level).toBe(LogLevel.INFO);
    expect(history[1].message).toBe('second');
    expect(history[1].level).toBe(LogLevel.WARN);
  });

  it('should limit history size', () => {
    for (let i = 0; i < 1500; i++) {
      logger.info(`message ${i}`);
    }
    const history = logger.getHistory();
    expect(history.length).toBeLessThanOrEqual(1000);
  });
});
