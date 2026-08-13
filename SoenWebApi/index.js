const cfg = require('./src/config');
const core = require('./src/core');
const db = require('./src/database');
const wa = require('./src/whatsapp');
const server = require('./src/server');
const s3 = require('./src/s3');
const mq = require('./src/mq');

// Load config
const conf = cfg.load();
core.init();

// Open database
try {
  db.open();
  core.log('INFO', 'Banco SQLite aberto');
} catch (e) {
  core.log('ERROR', `Falha ao abrir SQLite: ${e.message}`);
  try { db.close(); } catch {}
  process.exit(1);
}

// Clean old messages periodically
const ttl = (conf.messageTtlMinutes || 60) * 60 * 1000;
setInterval(() => db.cleanupMessages(conf.messageTtlMinutes || 60), Math.max(Math.min(ttl, 3600000), 60000));

// Periodic maintenance: WAL checkpoint, webhook cleanup, log rotation
const intervals = [
  setInterval(() => db.walCheckpoint(), 3600000),
  setInterval(() => db.cleanupWebhookEvents(30), 86400000),
  setInterval(() => core.checkLogRotation(), 3600000),
];

// Initialize S3/MinIO
try { s3.init(); } catch (e) { core.log('WARN', `S3 init: ${e.message}`); }

// Initialize Message Queue
try { mq.init(); } catch (e) { core.log('WARN', `MQ init: ${e.message}`); }

// Initialize WhatsApp clients
const clients = conf.clients || [];
if (clients.length === 0) {
  clients.push({ id: 'principal', name: conf.botName || 'Principal', enabled: true });
}
for (const c of clients) {
  if (c.enabled !== false) wa.initClient(c);
}

// Start HTTP + WebSocket server
server.start(conf.port || 3000).then(() => {
  core.log('INFO', `========================================`);
  core.log('INFO', `  DDS: ZAP-API v3.0 - ${conf.botName || 'Bot'}`);
  core.log('INFO', `  Modo: ${(conf.mode || 'single').toUpperCase()}`);
  core.log('INFO', `  Porta: ${conf.port || 3000}`);
  core.log('INFO', `  Clientes: ${clients.filter(c => c.enabled !== false).length}`);
  core.log('INFO', `  Auth: ${conf.apiKey ? 'Ativa' : 'Desativada'}`);
  core.log('INFO', `  AI: ${conf.openaiKey ? 'Ativa' : 'Desativada'}`);
  core.log('INFO', `  S3: ${s3.isEnabled() ? 'Ativo' : 'Desativado'}`);
  core.log('INFO', `  MQ: ${mq.isEnabled?.() ? 'Ativo' : 'Desativado'}`);
  core.log('INFO', `  Banco: data/zap.db`);
  core.log('INFO', `  Log: server.log`);
  core.log('INFO', `========================================`);
  core.log('INFO', `  API: http://localhost:${conf.port || 3000}`);
  core.log('INFO', `  WS:  ws://localhost:${conf.port || 3000}/ws`);
  core.log('INFO', `  Docs: http://localhost:${conf.port || 3000}/docs`);
  core.log('INFO', `  Admin: http://localhost:${conf.port || 3000}/admin`);
  core.log('INFO', `========================================`);
});

// Graceful shutdown
// Prevent crashes from unhandled rejections
process.on('unhandledRejection', (reason) => {
  core.log('ERROR', `Unhandled Rejection: ${reason?.message || reason}`);
});
process.on('uncaughtException', (err) => {
  core.log('ERROR', `Uncaught Exception: ${err.message}`);
  core.log('ERROR', err.stack);
  shutdown();
  setTimeout(() => process.exit(1), 3000);
});

function shutdown() {
  if (global.__shuttingDown) return;
  global.__shuttingDown = true;
  core.log('INFO', 'Desligando...');
  for (const i of intervals) clearInterval(i);
  try { mq.close(); } catch {}
  let saida = false;
  const forceExit = setTimeout(() => { if (!saida) process.exit(0); }, 5000);
  try { server.stop(() => { saida = true; clearTimeout(forceExit); process.exit(0); }); }
  catch (e) { core.log('ERROR', `Shutdown: ${e.message}`); process.exit(1); }
}
process.on('SIGINT', shutdown);
process.on('SIGTERM', shutdown);
