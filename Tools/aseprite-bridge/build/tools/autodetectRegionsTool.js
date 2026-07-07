import * as z from 'zod';
import { runAsepriteScript } from '../aseprite/runner.js';
const toolName = 'aseprite_autodetect_regions';
const toolDescription = 'Detecta regiões de conteúdo real numa imagem .aseprite achatada, sem depender de grid fixo. ' +
    'Usa projeção hierárquica (colunas, depois linhas dentro de cada coluna) para separar poses ' +
    'desenhadas lado a lado ou empilhadas verticalmente, mesmo com tamanhos irregulares. ' +
    'Use antes de aseprite_apply_slices para saber quais retângulos passar.';
const paramsSchema = z.object({
    path: z.string().describe('Caminho do arquivo .aseprite a analisar'),
    frame: z.number().int().positive().optional().describe('Frame a analisar (1-based). Default: 1'),
    minAlpha: z.number().int().min(0).max(255).optional().describe('Alpha mínimo para considerar um pixel "conteúdo". Default: 10'),
    minSize: z.number().int().positive().optional().describe('Largura/altura mínima (px) para uma região não ser descartada como ruído. Default: 3')
});
function parseRegionsOutput(stdout) {
    const regions = [];
    let total = 0;
    for (const line of stdout.split('\n')) {
        const trimmed = line.trim();
        if (trimmed.startsWith('REGION:')) {
            const [index, x, y, w, h] = trimmed.slice('REGION:'.length).split(':').map(Number);
            regions.push({ index, x, y, width: w, height: h });
        }
        else if (trimmed.startsWith('TOTAL:')) {
            total = Number(trimmed.slice('TOTAL:'.length));
        }
    }
    return { regions, total };
}
export function registerAutodetectRegionsTool(server, logger) {
    logger.info(`Registering tool: ${toolName}`);
    server.tool(toolName, toolDescription, paramsSchema.shape, async (params) => {
        try {
            logger.info(`Executing tool: ${toolName}`, params);
            const scriptParams = {};
            if (params.frame !== undefined)
                scriptParams.frame = String(params.frame);
            if (params.minAlpha !== undefined)
                scriptParams.minAlpha = String(params.minAlpha);
            if (params.minSize !== undefined)
                scriptParams.minSize = String(params.minSize);
            const stdout = await runAsepriteScript(logger, params.path, 'autodetect_regions.lua', scriptParams);
            const result = parseRegionsOutput(stdout);
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
