export enum LogLevel {
  DEBUG = 0,
  INFO = 1,
  WARN = 2,
  ERROR = 3,
}

export interface LogEntry {
  timestamp: Date;
  level: LogLevel;
  module: string;
  message: string;
  data?: unknown;
}

const MAX_HISTORY = 1000;

export class Logger {
  readonly module: string;
  private level: LogLevel = LogLevel.DEBUG;
  private history: LogEntry[] = [];

  constructor(module: string) {
    this.module = module;
  }

  setLevel(level: LogLevel): void {
    this.level = level;
  }

  debug(message: string, data?: unknown): void {
    this.log(LogLevel.DEBUG, message, data);
  }

  info(message: string, data?: unknown): void {
    this.log(LogLevel.INFO, message, data);
  }

  warn(message: string, data?: unknown): void {
    this.log(LogLevel.WARN, message, data);
  }

  error(message: string, data?: unknown): void {
    this.log(LogLevel.ERROR, message, data);
  }

  getHistory(): LogEntry[] {
    return [...this.history];
  }

  clearHistory(): void {
    this.history = [];
  }

  private log(level: LogLevel, message: string, data?: unknown): void {
    if (level < this.level) return;

    const entry: LogEntry = {
      timestamp: new Date(),
      level,
      module: this.module,
      message,
      data,
    };

    this.history.push(entry);
    if (this.history.length > MAX_HISTORY) {
      this.history = this.history.slice(-MAX_HISTORY);
    }

    const timestamp = this.formatTimestamp(entry.timestamp);
    const levelStr = `[${LogLevel[level]}]`;
    const moduleStr = `[${this.module}]`;

    const args: unknown[] = [
      `${timestamp} ${levelStr}`,
      moduleStr,
      message,
    ];
    if (data !== undefined) {
      args.push(data);
    }

    switch (level) {
      case LogLevel.DEBUG:
      case LogLevel.INFO:
        console.log(...args);
        break;
      case LogLevel.WARN:
        console.warn(...args);
        break;
      case LogLevel.ERROR:
        console.error(...args);
        break;
    }
  }

  private formatTimestamp(date: Date): string {
    const h = String(date.getHours()).padStart(2, '0');
    const m = String(date.getMinutes()).padStart(2, '0');
    const s = String(date.getSeconds()).padStart(2, '0');
    const ms = String(date.getMilliseconds()).padStart(3, '0');
    return `${h}:${m}:${s}.${ms}`;
  }
}

// Pre-configured loggers for each module
export const createLogger = (module: string): Logger => new Logger(module);
