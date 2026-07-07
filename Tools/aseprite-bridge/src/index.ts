import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { Logger, LogLevel } from './utils/logger.js';
import { registerInspectTool } from './tools/inspectTool.js';
import { registerPreviewPngTool } from './tools/previewPngTool.js';
import { registerAutodetectRegionsTool } from './tools/autodetectRegionsTool.js';
import { registerApplySlicesTool } from './tools/applySlicesTool.js';
import { registerPromoteToFramesTool } from './tools/promoteToFramesTool.js';

const serverLogger = new Logger('Server', LogLevel.INFO);
const toolLogger = new Logger('Tools', LogLevel.INFO);

const server = new McpServer(
  {
    name: 'Aseprite Bridge Server',
    version: '1.0.0'
  },
  {
    capabilities: {
      tools: {}
    }
  }
);

registerInspectTool(server, toolLogger);
registerPreviewPngTool(server, toolLogger);
registerAutodetectRegionsTool(server, toolLogger);
registerApplySlicesTool(server, toolLogger);
registerPromoteToFramesTool(server, toolLogger);

async function startServer() {
  try {
    const stdioTransport = new StdioServerTransport();
    await server.connect(stdioTransport);
    serverLogger.info('Aseprite Bridge MCP Server started');
  } catch (error) {
    serverLogger.error('Failed to start server', error);
    process.exit(1);
  }
}

let isShuttingDown = false;
async function shutdown() {
  if (isShuttingDown) return;
  isShuttingDown = true;

  try {
    serverLogger.info('Shutting down...');
    await server.close();
  } catch (error) {
    // Ignore errors during shutdown
  }
  process.exit(0);
}

startServer();

process.on('SIGINT', shutdown);
process.on('SIGTERM', shutdown);
process.on('SIGHUP', shutdown);

process.stdin.on('close', shutdown);
process.stdin.on('end', shutdown);
process.stdin.on('error', shutdown);

process.on('uncaughtException', (error: NodeJS.ErrnoException) => {
  if (error.code === 'EPIPE' || error.code === 'EOF' || error.code === 'ERR_USE_AFTER_CLOSE') {
    shutdown();
    return;
  }
  serverLogger.error('Uncaught exception', error);
  process.exit(1);
});

process.on('unhandledRejection', (reason) => {
  serverLogger.error('Unhandled rejection', reason);
  process.exit(1);
});
