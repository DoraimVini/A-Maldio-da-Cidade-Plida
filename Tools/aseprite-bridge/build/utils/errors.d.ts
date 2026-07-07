export declare enum ErrorType {
    VALIDATION = "VALIDATION",
    ASEPRITE_EXECUTION = "ASEPRITE_EXECUTION",
    CONFIGURATION = "CONFIGURATION"
}
export declare class AsepriteBridgeError extends Error {
    readonly type: ErrorType;
    constructor(type: ErrorType, message: string);
}
