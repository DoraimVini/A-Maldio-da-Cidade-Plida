import { Logger } from '../utils/logger.js';
/**
 * Roda um script Lua bundlado em scripts/ contra um arquivo .aseprite, em modo batch
 * (sem abrir a UI). Parâmetros viram `--script-param chave=valor`, lidos no Lua via
 * `app.params["chave"]`.
 */
export declare function runAsepriteScript(logger: Logger, filePath: string, scriptName: string, params?: Record<string, string>): Promise<string>;
/**
 * Exporta um frame (ou o composto achatado) de um .aseprite como PNG, sem precisar de
 * script Lua — usa flags nativas do CLI (`--frame-range`, `--trim-sprite`, `--save-as`).
 */
export declare function exportFramePng(logger: Logger, filePath: string, outputPath: string, options?: {
    frame?: number;
    trim?: boolean;
}): Promise<void>;
