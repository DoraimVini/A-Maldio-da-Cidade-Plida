import * as z from 'zod';
import path from 'path';
import { promises as fs } from 'fs';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';
import { Logger } from '../utils/logger.js';
import { runAsepriteScript } from '../aseprite/runner.js';
import { AsepriteBridgeError, ErrorType } from '../utils/errors.js';

const toolName = 'aseprite_promote_slices_to_frames';
const toolDescription =
  'Converte cada Slice existente (criada antes via aseprite_apply_slices) num Frame real, ' +
  'recortando a região correspondente do frame achatado original e removendo esse frame no ' +
  'final. Resultado: um .aseprite com frames de verdade, pronto para o importer nativo ' +
  'com.unity.2d.aseprite fatiar sem passo manual extra. Por padrão salva num ARQUIVO NOVO ' +
  '(<nome>.sliced.aseprite) — só sobrescreve o original se outputPath for igual ao path de entrada.';

const tagGroupSchema = z.object({
  name: z.string().describe('Nome da Tag (ex.: "Idle", "Andando")'),
  fromIndex: z.number().int().positive().describe('Índice (1-based) do primeiro slice do grupo, na ordem em que foram passados a aseprite_apply_slices'),
  toIndex: z.number().int().positive().describe('Índice (1-based) do último slice do grupo')
});

const paramsSchema = z.object({
  path: z.string().describe('Caminho do arquivo .aseprite de origem (deve já ter Slices aplicadas)'),
  outputPath: z.string().optional().describe('Caminho de saída. Se omitido, usa "<nome-sem-extensao>.sliced.aseprite" ao lado do arquivo de origem.'),
  sourceFrame: z.number().int().positive().optional().describe('Frame achatado a recortar e remover. Default: 1'),
  tagGroups: z.array(tagGroupSchema).optional().describe('Agrupa os frames promovidos em Tags nomeadas')
});

function encodeTagGroups(groups: z.infer<typeof tagGroupSchema>[] | undefined): string {
  if (!groups || groups.length === 0) return '';
  for (const g of groups) {
    if (g.name.includes(',') || g.name.includes(';')) {
      throw new AsepriteBridgeError(
        ErrorType.VALIDATION,
        `Nome de tag inválido: '${g.name}'. Não pode conter ',' ou ';'.`
      );
    }
  }
  return groups.map((g) => `${g.name},${g.fromIndex},${g.toIndex}`).join(';');
}

export function registerPromoteToFramesTool(server: McpServer, logger: Logger) {
  logger.info(`Registering tool: ${toolName}`);

  server.tool(toolName, toolDescription, paramsSchema.shape, async (params: z.infer<typeof paramsSchema>) => {
    try {
      logger.info(`Executing tool: ${toolName}`, params);

      const parsed = path.parse(params.path);
      const outputPath = params.outputPath ?? path.join(parsed.dir, `${parsed.name}.sliced${parsed.ext}`);

      if (path.resolve(outputPath) === path.resolve(params.path)) {
        const backupPath = `${params.path}.bak`;
        await fs.copyFile(params.path, backupPath);
        logger.info(`Sobrescrita in-place solicitada; backup criado em ${backupPath}`);
      }

      const stdout = await runAsepriteScript(logger, params.path, 'promote_to_frames.lua', {
        outputPath,
        sourceFrame: String(params.sourceFrame ?? 1),
        tagGroups: encodeTagGroups(params.tagGroups)
      });

      const promotedMatch = stdout.match(/PROMOTED_FRAMES:(\d+)/);
      const totalMatch = stdout.match(/TOTAL_FRAMES:(\d+)/);
      const tagsMatch = stdout.match(/TAGS_CREATED:(\d+)/);

      const response: CallToolResult = {
        content: [{
          type: 'text',
          text: JSON.stringify({
            outputPath,
            promotedFrames: promotedMatch ? Number(promotedMatch[1]) : 0,
            totalFrames: totalMatch ? Number(totalMatch[1]) : 0,
            tagsCreated: tagsMatch ? Number(tagsMatch[1]) : 0
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
