import { execFile } from 'child_process';
import { promisify } from 'util';
import path from 'path';
import { fileURLToPath } from 'url';
import { AsepriteBridgeError, ErrorType } from '../utils/errors.js';
const execFileAsync = promisify(execFile);
// Tools/aseprite-bridge/build/aseprite/runner.js -> Tools/aseprite-bridge/scripts/
const scriptsDir = path.join(path.dirname(fileURLToPath(import.meta.url)), '..', '..', 'scripts');
function resolveExePath() {
    const exePath = process.env.ASEPRITE_EXE_PATH;
    if (!exePath) {
        throw new AsepriteBridgeError(ErrorType.CONFIGURATION, "Variável de ambiente ASEPRITE_EXE_PATH não configurada. Defina-a na entrada 'aseprite-bridge' do .mcp.json apontando para o executável do Aseprite (ex.: C:\\aseprite\\build\\bin\\aseprite.exe).");
    }
    return exePath;
}
/**
 * Roda um script Lua bundlado em scripts/ contra um arquivo .aseprite, em modo batch
 * (sem abrir a UI). Parâmetros viram `--script-param chave=valor`, lidos no Lua via
 * `app.params["chave"]`.
 */
export async function runAsepriteScript(logger, filePath, scriptName, params = {}) {
    const exePath = resolveExePath();
    const scriptPath = path.join(scriptsDir, scriptName);
    const resolvedFile = path.resolve(filePath);
    const args = ['-b', resolvedFile];
    for (const [key, value] of Object.entries(params)) {
        args.push('--script-param', `${key}=${value}`);
    }
    args.push('--script', scriptPath);
    logger.debug(`Executando aseprite ${args.join(' ')}`);
    try {
        const { stdout, stderr } = await execFileAsync(exePath, args);
        if (stderr && stderr.trim().length > 0) {
            logger.warn(`Aseprite stderr (${scriptName})`, stderr);
        }
        return stdout;
    }
    catch (error) {
        const message = error instanceof Error ? error.message : String(error);
        throw new AsepriteBridgeError(ErrorType.ASEPRITE_EXECUTION, `Falha ao rodar script '${scriptName}' contra '${filePath}': ${message}`);
    }
}
/**
 * Exporta um frame (ou o composto achatado) de um .aseprite como PNG, sem precisar de
 * script Lua — usa flags nativas do CLI (`--frame-range`, `--trim-sprite`, `--save-as`).
 */
export async function exportFramePng(logger, filePath, outputPath, options = {}) {
    const exePath = resolveExePath();
    const resolvedFile = path.resolve(filePath);
    const resolvedOutput = path.resolve(outputPath);
    const args = ['-b', resolvedFile];
    if (options.frame !== undefined) {
        args.push('--frame-range', `${options.frame},${options.frame}`);
    }
    if (options.trim) {
        args.push('--trim-sprite');
    }
    args.push('--save-as', resolvedOutput);
    logger.debug(`Exportando preview: aseprite ${args.join(' ')}`);
    try {
        await execFileAsync(exePath, args);
    }
    catch (error) {
        const message = error instanceof Error ? error.message : String(error);
        throw new AsepriteBridgeError(ErrorType.ASEPRITE_EXECUTION, `Falha ao exportar preview PNG de '${filePath}': ${message}`);
    }
}
