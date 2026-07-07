import * as z from 'zod';
import { runAsepriteScript } from '../aseprite/runner.js';
const toolName = 'aseprite_inspect';
const toolDescription = 'Inspeciona um arquivo .aseprite: dimensões, número de frames, layers, tags e slices existentes. Primeiro passo antes de fatiar qualquer spritesheet.';
const paramsSchema = z.object({
    path: z.string().describe('Caminho (relativo ao repo ou absoluto) do arquivo .aseprite a inspecionar')
});
function parseInspectOutput(stdout) {
    const result = { width: 0, height: 0, frames: 0, layers: [], tags: [], slices: [] };
    for (const line of stdout.split('\n')) {
        const trimmed = line.trim();
        if (trimmed.startsWith('WIDTH:')) {
            result.width = Number(trimmed.slice('WIDTH:'.length));
        }
        else if (trimmed.startsWith('HEIGHT:')) {
            result.height = Number(trimmed.slice('HEIGHT:'.length));
        }
        else if (trimmed.startsWith('FRAMES:')) {
            result.frames = Number(trimmed.slice('FRAMES:'.length));
        }
        else if (trimmed.startsWith('LAYER:')) {
            result.layers.push(trimmed.slice('LAYER:'.length));
        }
        else if (trimmed.startsWith('TAG:')) {
            const [name, fromFrame, toFrame] = trimmed.slice('TAG:'.length).split(':');
            result.tags.push({ name, fromFrame: Number(fromFrame), toFrame: Number(toFrame) });
        }
        else if (trimmed.startsWith('SLICE:')) {
            const [name, x, y, w, h] = trimmed.slice('SLICE:'.length).split(':');
            result.slices.push({ name, x: Number(x), y: Number(y), width: Number(w), height: Number(h) });
        }
    }
    return result;
}
export function registerInspectTool(server, logger) {
    logger.info(`Registering tool: ${toolName}`);
    server.tool(toolName, toolDescription, paramsSchema.shape, async (params) => {
        try {
            logger.info(`Executing tool: ${toolName}`, params);
            const stdout = await runAsepriteScript(logger, params.path, 'inspect.lua');
            const result = parseInspectOutput(stdout);
            const response = {
                content: [{ type: 'text', text: JSON.stringify(result, null, 2) }]
            };
            logger.info(`Tool execution successful: ${toolName}`);
            return response;
        }
        catch (error) {
            logger.error(`Tool execution failed: ${toolName}`, error);
            throw error;
        }
    });
}
