export var ErrorType;
(function (ErrorType) {
    ErrorType["VALIDATION"] = "VALIDATION";
    ErrorType["ASEPRITE_EXECUTION"] = "ASEPRITE_EXECUTION";
    ErrorType["CONFIGURATION"] = "CONFIGURATION";
})(ErrorType || (ErrorType = {}));
export class AsepriteBridgeError extends Error {
    type;
    constructor(type, message) {
        super(message);
        this.type = type;
        this.name = 'AsepriteBridgeError';
    }
}
