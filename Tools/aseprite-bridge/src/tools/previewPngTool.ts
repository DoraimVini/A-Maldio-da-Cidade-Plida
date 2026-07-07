import * as z from 'zod';
import path from 'path';
import os from 'os';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';
import { Logger } from '../utils/logger.js';
import { exportFramePng } from '../aseprite/runner.js';

const toolName = 'aseprite_preview_png';
const toolDescription = 'Exporta um frame de um .aseprite (ou o composto achatado, por padrão o frame 1) como PNG num diretório temporário, para inspeção visual antes de decidir o fatiamento.';
const paramsSchema = z.object({
  path: z.string().describe('Caminho do arquivo .aseprite a exportar'),
  frame: z.number().int().positive().optional().describe('Número do frame a exportar (1-based). Se omitido, exporta o frame 1.'),
  trim: z.boolean().optional().describe('Se true, recorta o PNG exportado para o bounding box do conteúdo visível (--trim-sprite)')
});

export function registerPreviewPngTool(server: McpServer, logger: Logger) {
  logger.info(`Registering tool: ${toolName}`);

  server.tool(toolName, toolDescription, paramsSchema.shape, async (params: z.infer<typeof paramsSchema>) => {
    try {
      logger.info(`Executing tool: ${toolName}`, params);

      const baseName = path.basename(params.path, path.extname(params.path));
      const outputPath = path.join(os.tmpdir(), `${baseName}_frame${params.frame ?? 1}_${Date.now()}.png`);

      await exportFramePng(logger, params.path, outputPath, { frame: params.frame, trim: params.trim });

      const response: CallToolResult = {
        content: [{ type: 'text', text: `Preview exportado em: ${outputPath}` }]
      };
      logger.info(`Tool execution successful: ${toolName}`);
      return response;
    } catch (error) {
      logger.error(`Tool execution failed: ${toolName}`, error);
      throw error;
    }
  });
}
