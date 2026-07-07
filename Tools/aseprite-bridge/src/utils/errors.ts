export enum ErrorType {
  VALIDATION = 'VALIDATION',
  ASEPRITE_EXECUTION = 'ASEPRITE_EXECUTION',
  CONFIGURATION = 'CONFIGURATION'
}

export class AsepriteBridgeError extends Error {
  public readonly type: ErrorType;

  constructor(type: ErrorType, message: string) {
    super(message);
    this.type = type;
    this.name = 'AsepriteBridgeError';
  }
}
