export enum LogLevel {
  DEBUG = 0,
  INFO = 1,
  WARN = 2,
  ERROR = 3
}

const isLoggingEnabled = process.env.LOGGING === 'true';

export class Logger {
  private level: LogLevel;
  private prefix: string;

  constructor(prefix: string, level: LogLevel = LogLevel.INFO) {
    this.prefix = prefix;
    this.level = level;
  }

  debug(message: string, data?: unknown) {
    this.log(LogLevel.DEBUG, message, data);
  }

  info(message: string, data?: unknown) {
    this.log(LogLevel.INFO, message, data);
  }

  warn(message: string, data?: unknown) {
    this.log(LogLevel.WARN, message, data);
  }

  error(message: string, error?: unknown) {
    this.log(LogLevel.ERROR, message, error);
  }

  private log(level: LogLevel, message: string, data?: unknown) {
    if (level < this.level || !isLoggingEnabled) return;

    const timestamp = new Date().toISOString();
    const levelStr = LogLevel[level];
    const logMessage = `[${timestamp}] [${levelStr}] [${this.prefix}] ${message}`;

    if (data !== undefined) {
      console.error(logMessage, data);
    } else {
      console.error(logMessage);
    }
  }
}
