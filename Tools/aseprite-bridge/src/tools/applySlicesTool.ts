import * as z from 'zod';
import { promises as fs } from 'fs';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';
import { Logger } from '../utils/logger.js';
import { runAsepriteScript } from '../aseprite/runner.js';
import { AsepriteBridgeError, ErrorType } from '../utils/errors.js';

const toolName = 'aseprite_apply_slices';
const toolDescription =
  'Adiciona Slices (metadata, não mexe nos pixels) num .aseprite nas coordenadas informadas. ' +
  'Não-destrutivo e reversível na UI do Aseprite. Faz backup automático (.bak) do arquivo antes ' +
  'de salvar. Use aseprite_autodetect_regions antes para descobrir os retângulos.';

const sliceSchema = z.object({
  name: z.string().describe('Nome da slice (vira o nome do frame/sprite depois de promovido)'),
  x: z.number().int(),
  y: z.number().int(),
  width: z.number().int().positive(),
  height: z.number().int().positive()
});

const paramsSchema = z.object({
  path: z.string().describe('Caminho do arquivo .aseprite a modificar'),
  slices: z.array(sliceSchema).min(1).describe('Lista de retângulos a virar Slice'),
  replace: z.boolean().optional().describe('Se true, remove todas as slices existentes antes de aplicar as novas (default: false, soma às existentes)')
});

function encodeSlices(slices: z.infer<typeof sliceSchema>[]): string {
  return slices
    .map((s) => `${s.name},${s.x},${s.y},${s.width},${s.height}`)
    .join(';');
}

export function registerApplySlicesTool(server: McpServer, logger: Logger) {
  logger.info(`Registering tool: ${toolName}`);

  server.tool(toolName, toolDescription, paramsSchema.shape, async (params: z.infer<typeof paramsSchema>) => {
    try {
      logger.info(`Executing tool: ${toolName}`, params);

      for (const slice of params.slices) {
        if (slice.name.includes(',') || slice.name.includes(';')) {
          throw new AsepriteBridgeError(
            ErrorType.VALIDATION,
            `Nome de slice inválido: '${slice.name}'. Não pode conter ',' ou ';' (usados como delimitador ao chamar o Aseprite).`
          );
        }
      }

      const backupPath = `${params.path}.bak`;
      await fs.copyFile(params.path, backupPath);

      const stdout = await runAsepriteScript(logger, params.path, 'apply_slices.lua', {
        slices: encodeSlices(params.slices),
        replace: params.replace ? 'true' : 'false'
      });

      const createdMatch = stdout.match(/CREATED:(\d+)/);
      const totalMatch = stdout.match(/TOTAL_SLICES:(\d+)/);

      const response: CallToolResult = {
        content: [{
          type: 'text',
          text: JSON.stringify({
            created: createdMatch ? Number(createdMatch[1]) : 0,
            totalSlices: totalMatch ? Number(totalMatch[1]) : 0,
            backupPath
          }, null, 2)
        }]
      };
      logger.info(`Tool execution successful: ${toolName}`);
      return response;
    } catch (error) {
      logger.error(`Tool execution failed: ${toolName}`, error);
      throw error;
    }
  });
}
