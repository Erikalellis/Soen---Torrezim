const express = require('express');
const rateLimit = require('express-rate-limit');
const helmet = require('helmet');
const cors = require('cors');
const { WebSocketServer } = require('ws');
const http = require('http');
const fs = require('fs');
const path = require('path');
const swaggerUi = require('swagger-ui-express');
const cfg = require('./config');
const db = require('./database');
const core = require('./core');
const wa = require('./whatsapp');
const jwt = require('./jwt');

const app = express();
const server = http.createServer(app);

app.use(helmet({ contentSecurityPolicy: false, crossOriginEmbedderPolicy: false }));
const CORS_ORIGIN = process.env.CORS_ORIGIN || 'http://localhost:3000,http://127.0.0.1:3000';
const corsOrigins = CORS_ORIGIN.split(',').map(s => s.trim()).filter(Boolean);
app.use(cors({ origin: corsOrigins.includes('*') ? '*' : corsOrigins }));
app.use(express.json({ limit: '10mb' }));

// Request timeout (30s)
app.use((req, res, next) => {
  res.setTimeout(30000, () => {
    res.status(503).json({ error: 'Tempo limite excedido' });
  });
  next();
});

const apiLimiter = rateLimit({
  windowMs: (cfg.get().rateLimit?.windowMinutes || 15) * 60 * 1000,
  max: cfg.get().rateLimit?.maxRequests || 100,
  message: { error: 'Muitas requisicoes' },
  validate: false,
});
app.use('/api', apiLimiter);

// ================================================================
// WEBSOCKET (Socket.io style via WS)
// ================================================================
const wss = new WebSocketServer({ server, path: '/ws', maxPayload: 1024 * 1024 });
const wsClients = new Set();
const WS_MAX_CLIENTS = 200;

wss.on('connection', (ws, req) => {
  if (wsClients.size >= WS_MAX_CLIENTS) {
    ws.close(4003, 'Muitas conexoes');
    return;
  }
  const conf = cfg.get();
  const globalKey = conf.apiKey || process.env.API_KEY;
  if (globalKey) {
    const params = new URL(req.url, 'http://localhost').searchParams;
    const token = params.get('token') || params.get('apiKey');
    if (!token || token !== globalKey) {
      ws.close(4001, 'API key required');
      return;
    }
  }
  wsClients.add(ws);
  ws.isAlive = true;
  ws.on('pong', () => { ws.isAlive = true; });
  ws.on('close', () => wsClients.delete(ws));
  ws.on('error', () => wsClients.delete(ws));
});

// Ping/pong para limpar conexoes mortas
const WS_PING_INTERVAL = setInterval(() => {
  for (const ws of wsClients) {
    if (ws.isAlive === false) {
      try { ws.terminate(); } catch {}
      wsClients.delete(ws);
      continue;
    }
    ws.isAlive = false;
    try { ws.ping(); } catch { try { ws.terminate(); } catch {} wsClients.delete(ws); }
  }
}, 30000);

core.setBroadcast((event, data) => {
  const msg = JSON.stringify({ event, data, timestamp: Date.now() });
  for (const ws of wsClients) {
    try { ws.send(msg); } catch { wsClients.delete(ws); }
  }
});

// ================================================================
// MIDDLEWARE DE AUTENTICACAO
// ================================================================
function auth(req, res, next) {
  const conf = cfg.get();
  const globalKey = conf.apiKey || process.env.API_KEY;
  const header = req.headers['x-api-key'] || req.headers['authorization']?.replace('Bearer ', '');

  if (!globalKey) return next();
  if (!header) return res.status(401).json({ error: 'API key obrigatoria' });

  if (header === globalKey) {
    req.authType = 'global';
    return next();
  }

  const result = jwt.verifyToken(header);
  if (result.valid) {
    req.authType = 'jwt';
    req.authInstance = result.instance;
    return next();
  }

  return res.status(401).json({ error: 'API key ou token invalido' });
}

function requireClient(req, res, next) {
  req.clientId = req.params.clientId || req.body?.clientId || 'principal';
  const c = wa.getClient(req.clientId);
  if (!c && req.path !== '/status' && req.path !== '/qrcode') {
    return res.status(404).json({ error: `Cliente '${req.clientId}' nao encontrado` });
  }
  next();
}

function validate(schema) {
  return (req, res, next) => {
    const fields = {};
    for (const [key, rules] of Object.entries(schema)) {
      fields[key] = { ...rules, value: req.body[key] };
    }
    const errors = core.validate(fields);
    if (errors.length) return res.status(400).json({ error: errors.join('; ') });
    next();
  };
}

// ================================================================
// ROTAS
// ================================================================

// Swagger docs
const swaggerDoc = JSON.parse(fs.readFileSync(path.join(__dirname, '..', 'swagger.json'), 'utf-8'));
app.use('/docs', auth, swaggerUi.serve, swaggerUi.setup(swaggerDoc, { customSiteTitle: 'DDS: ZAP-API v3 - Documentacao' }));
app.get('/api-docs', auth, (req, res) => res.json(swaggerDoc));

// Health
app.get('/', (req, res) => res.json({ app: cfg.get().botName || 'DDS: ZAP-API', version: '3.0' }));
app.get('/ping', (req, res) => res.json({ status: 'online', timestamp: new Date().toISOString() }));

// Bootstrap local: entrega a API key ao painel web (servidor preso em 127.0.0.1,
// CORS restrito a localhost). Sem auth porque e o ponto de entrada do proprio painel.
app.get('/api/auth/bootstrap', (req, res) => {
  const conf = cfg.get();
  res.json({ apiKey: conf.apiKey || process.env.API_KEY || '', jwt: false });
});

// Web Admin (SPA)
const PUBLIC_DIR = path.join(__dirname, '..', 'public');
if (fs.existsSync(PUBLIC_DIR)) {
  app.use('/admin', express.static(PUBLIC_DIR, { extensions: ['html'] }));
  app.get('/admin/*', (req, res) => {
    const fp = path.join(PUBLIC_DIR, 'index.html');
    if (fs.existsSync(fp)) res.sendFile(fp); else res.redirect('/admin');
  });
  core.log('INFO', `Painel web: /admin (${PUBLIC_DIR})`);
}

// --- JWT ---
app.post('/api/auth/token', (req, res) => {
  try {
    const conf = cfg.get();
    const globalKey = conf.apiKey || process.env.API_KEY;
    const header = req.headers['x-api-key'] || req.body?.apiKey;
    if (globalKey && header !== globalKey) return res.status(401).json({ error: 'API key invalida' });
    const instanceId = req.body?.instanceId || 'principal';
    const token = jwt.generateToken(instanceId);
    res.json({ success: true, token, instanceId, expiresIn: '30d' });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.get('/api/auth/check', auth, (req, res) => {
  res.json({ valid: true, type: req.authType, instance: req.authInstance });
});

// --- Status ---
app.get('/status', auth, (req, res) => {
  const conf = cfg.get();
  const all = wa.getAllClients();
  res.json({
    mode: conf.mode, botEnabled: conf.botEnabled, vacationMode: conf.vacationMode, botName: conf.botName,
    uptime: Math.floor((Date.now() - global.START_TIME) / 1000),
    uptimeFormatted: core.fmtUptime(Math.floor((Date.now() - global.START_TIME) / 1000)),
    aiProvider: conf.aiProvider || 'openai',
    ai: !!(conf.openaiKey || conf.aiProvider === 'ollama'),
    clients: all.map(c => ({
      id: c.id, connected: c.connected, ready: c.ready, qrcode: c.qrCode,
      stats: db.getStats(c.id), humanModeCount: db.getHumanList(c.id).length,
      blacklistedCount: db.getBlacklist(c.id).length, storedMessages: db.countMessages(c.id),
    })),
  });
});

// QR Code
app.get('/qrcode', auth, (req, res) => {
  const cid = req.query.client || 'principal';
  const c = wa.getClient(cid);
  if (!c) return res.status(404).json({ error: 'Cliente nao encontrado' });
  if (c.connected) return res.json({ connected: true, client: cid });
  res.json({ connected: false, client: cid, qr: c.qrCode || null });
});

app.post('/api/client/:clientId/reconnect', auth, async (req, res) => {
  try {
    const cid = req.params.clientId;
    const c = wa.getClient(cid);
    if (!c) return res.status(404).json({ error: 'Cliente nao encontrado' });
    const waClient = c.client;
    if (waClient) try { await waClient.destroy(); } catch {}
    const conf = cfg.get();
    const clientCfg = (conf.clients || []).find(x => x.id === cid) || { id: cid, name: cid };
    wa.initClient(clientCfg);
    core.log('INFO', `Reconectando cliente ${cid}`);
    res.json({ success: true, msg: `Reconectando ${cid}` });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- ENVIO ---
app.post('/api/send/text', auth, requireClient, validate({ number: { required: true, type: 'string', minLength: 10 }, message: { required: true, type: 'string', minLength: 1 } }), async (req, res) => {
  try {
    const waClient = wa.getWAClient(req.clientId);
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const r = await waClient.sendMessage(core.formatPhone(req.body.number), req.body.message);
    db.incrementStat(req.clientId, 'sent');
    core.emitWebhook(core.WEBHOOK_EVENTS.SEND_MESSAGE, { to: req.body.number, message: req.body.message, id: r.id.id }, req.clientId);
    res.json({ success: true, id: r.id.id, clientId: req.clientId });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/send/media', auth, requireClient, validate({ number: { required: true, type: 'string', minLength: 10 } }), async (req, res) => {
  try {
    const waClient = wa.getWAClient(req.clientId);
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const { number, url, fileName, caption } = req.body;
    if (!url && !fileName) return res.status(400).json({ error: 'url ou fileName obrigatorio' });
    let media;
    if (url) {
      if (!/^https?:\/\/.+/.test(url)) return res.status(400).json({ error: 'URL invalida' });
      media = await wa.MessageMedia.fromUrl(url, { unsafeMime: true });
    } else {
      const fp = path.join(wa.MEDIA_DIR, path.basename(fileName));
      if (!fs.existsSync(fp)) return res.status(404).json({ error: `Arquivo ${fileName} nao encontrado` });
      media = wa.MessageMedia.fromFilePath(fp);
    }
    await waClient.sendMessage(core.formatPhone(number), media, { caption: caption || '' });
    db.incrementStat(req.clientId, 'sent');
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- Broadcast ---
app.post('/api/send/broadcast', auth, requireClient, validate({
  numbers: { required: true, type: 'array', minItems: 1 },
  message: { required: true, type: 'string', minLength: 1 },
}), async (req, res) => {
  try {
    const waClient = wa.getWAClient(req.clientId);
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const { numbers, message } = req.body;
    if (numbers.length > 100) return res.status(400).json({ error: 'Maximo 100 numeros por broadcast' });
    const results = [];
    for (const num of numbers) {
      try {
        await waClient.sendMessage(core.formatPhone(num), message);
        db.incrementStat(req.clientId, 'sent');
        results.push({ number: num, success: true });
      } catch (e) {
        results.push({ number: num, success: false, error: e.message });
      }
    }
    const sent = results.filter(r => r.success).length;
    const failed = results.filter(r => !r.success).length;
    core.emitWebhook(core.WEBHOOK_EVENTS.SEND_MESSAGE, { numbers, message, results, sent, failed }, req.clientId);
    res.json({ success: true, sent, failed, results });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/send/poll', auth, requireClient, validate({
  number: { required: true, type: 'string' }, question: { required: true, type: 'string' }, options: { required: true, type: 'array', minItems: 2 },
}), async (req, res) => {
  try {
    const waClient = wa.getWAClient(req.clientId);
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const r = await waClient.sendMessage(core.formatPhone(req.body.number), { type: 'poll', poll: { name: req.body.question, options: req.body.options.map(o => ({ name: o })) } });
    db.incrementStat(req.clientId, 'sent');
    res.json({ success: true, id: r.id.id });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/send/location', auth, requireClient, async (req, res) => {
  try {
    const waClient = wa.getWAClient(req.clientId);
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const { number, latitude, longitude, title } = req.body;
    if (!number || latitude === undefined || longitude === undefined) return res.status(400).json({ error: 'number, latitude e longitude obrigatorios' });
    const loc = { latitude: parseFloat(latitude), longitude: parseFloat(longitude), title: title || '' };
    await waClient.sendMessage(core.formatPhone(number), loc);
    db.incrementStat(req.clientId, 'sent');
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/send/contact', auth, requireClient, async (req, res) => {
  try {
    const waClient = wa.getWAClient(req.clientId);
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const { number, contacts } = req.body;
    if (!number || !contacts) return res.status(400).json({ error: 'number e contacts obrigatorios' });
    const vcard = 'BEGIN:VCARD\nVERSION:3.0\n' + contacts.map(c => `FN:${c.name || c.number}\nTEL;TYPE=CELL:${c.number}`).join('\n') + '\nEND:VCARD';
    const media = new wa.MessageMedia('text/x-vcard', Buffer.from(vcard).toString('base64'), 'contact.vcf');
    await waClient.sendMessage(core.formatPhone(number), media, { caption: 'Contato' });
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/react', auth, requireClient, async (req, res) => {
  try {
    const waClient = wa.getWAClient(req.clientId);
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const { number, messageId, emoji } = req.body;
    if (!number || !messageId || !emoji) return res.status(400).json({ error: 'number, messageId e emoji obrigatorios' });
    await waClient.sendMessage(core.formatPhone(number), { react: { messageId, emoji } });
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/broadcast', auth, requireClient, async (req, res) => {
  try {
    const waClient = wa.getWAClient(req.clientId);
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const { numbers, message, mediaUrl } = req.body;
    if (!numbers || !Array.isArray(numbers) || !numbers.length) return res.status(400).json({ error: 'numbers (array) obrigatorio' });
    const results = [];
    for (const num of numbers) {
      try {
        if (mediaUrl) {
          const media = await wa.MessageMedia.fromUrl(mediaUrl, { unsafeMime: true });
          await waClient.sendMessage(core.formatPhone(num), media, { caption: message || '' });
        } else await waClient.sendMessage(core.formatPhone(num), message);
        db.incrementStat(req.clientId, 'sent');
        results.push({ number: num, success: true });
      } catch (e) { results.push({ number: num, success: false, error: e.message }); }
    }
    res.json({ success: true, results });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/send/ai', auth, requireClient, async (req, res) => {
  try {
    const { number, prompt } = req.body;
    if (!number || !prompt) return res.status(400).json({ error: 'number e prompt obrigatorios' });
    const waClient = wa.getWAClient(req.clientId);
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const aiResp = await wa.callAI(prompt, cfg.get().botName || 'Bot');
    if (!aiResp) return res.status(503).json({ error: 'IA nao disponivel (configure OPENAI_API_KEY)' });
    await waClient.sendMessage(core.formatPhone(number), aiResp);
    db.incrementStat(req.clientId, 'sent');
    res.json({ success: true, response: aiResp });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- MENSAGENS ---
app.get('/api/messages', auth, (req, res) => {
  try {
    const cid = req.query.client || '';
    const limit = Math.min(parseInt(req.query.limit) || 50, 500);
    const offset = parseInt(req.query.offset) || 0;
    const from = req.query.from || '';
    const msgs = db.getMessages(cid, limit, offset, from);
    const total = db.countMessages(cid);
    res.json({ total, returned: msgs.length, messages: msgs.map(m => ({ ...m, formattedTime: new Date(m.timestamp > 1e12 ? m.timestamp : m.timestamp * 1000).toISOString() })) });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.get('/api/messages/export', auth, (req, res) => {
  try {
    const cid = req.query.client || '';
    const msgs = db.exportMessages(cid);
    res.setHeader('Content-Type', 'application/json');
    res.setHeader('Content-Disposition', `attachment; filename=chat-${cid || 'all'}-${Date.now()}.json`);
    res.json(msgs);
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.delete('/api/messages', auth, (req, res) => {
  try {
    db.deleteMessages(req.body?.clientId || '');
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- Exportar conversas (CSV ou JSON) ---
app.get('/api/export/:clientId', auth, (req, res) => {
  try {
    const cid = req.params.clientId;
    const format = req.query.format || 'json';
    const msgs = db.exportMessages(cid === 'all' ? '' : cid);
    const filename = `chat-${cid}-${Date.now()}`;
    if (format === 'csv') {
      const header = 'id,client_id,from_number,body,has_media,timestamp,stored_at\n';
      const rows = msgs.map(m => `${m.id || ''},"${m.client_id || ''}","${(m.from_number || '').replace(/"/g, '""')}","${(m.body || '').replace(/"/g, '""')}",${m.has_media || 0},${m.timestamp || ''},${m.stored_at || ''}`).join('\n');
      res.setHeader('Content-Type', 'text/csv; charset=utf-8');
      res.setHeader('Content-Disposition', `attachment; filename=${filename}.csv`);
      res.send('\uFEFF' + header + rows);
    } else {
      res.setHeader('Content-Type', 'application/json');
      res.setHeader('Content-Disposition', `attachment; filename=${filename}.json`);
      res.json({ exportedAt: new Date().toISOString(), count: msgs.length, messages: msgs });
    }
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- CONVERSAS ---
app.get('/api/conversations', auth, (req, res) => {
  try {
    const cid = req.query.client || '';
    const msgs = db.getMessages(cid, 5000);
    const convs = {};
    for (const m of msgs) {
      const key = m.from_number;
      if (!convs[key]) convs[key] = { number: key, count: 0, lastMsg: '', lastTime: 0, messages: [] };
      convs[key].count++;
      convs[key].lastMsg = m.body;
      convs[key].lastTime = m.timestamp;
      convs[key].messages.push(m);
    }
    const list = Object.values(convs).sort((a, b) => b.lastTime - a.lastTime);
    res.json({ conversations: list });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- CONFIGURACAO ---
const SENSITIVE_CONFIG_FIELDS = ['apiKey', 'openaiKey', 'webhookSecret', 'accessKey', 'secretKey'];

function redactConfig(obj) {
  const redacted = { ...obj };
  for (const key of SENSITIVE_CONFIG_FIELDS) {
    if (redacted[key]) redacted[key] = '***';
  }
  if (redacted.s3) redacted.s3 = { ...redacted.s3 };
  if (redacted.s3?.accessKey) redacted.s3.accessKey = '***';
  if (redacted.s3?.secretKey) redacted.s3.secretKey = '***';
  return redacted;
}

app.get('/api/config', auth, (req, res) => {
  try { res.json(redactConfig(JSON.parse(fs.readFileSync(cfg.CONFIG_PATH, 'utf-8')))); }
  catch { res.json(redactConfig(cfg.get())); }
});

const ALLOWED_CONFIG_FIELDS = ['mode', 'botName', 'botEnabled', 'vacationMode', 'vacationMessage', 'controllerNumber', 'humanResetHours', 'messageTtlMinutes', 'openaiModel', 'aiProvider', 'ollamaUrl', 'ollamaModel', 'webhookUrl', 'proxy', 'rateLimit', 'clients', 'messageQueue'];

app.put('/api/config', auth, (req, res) => {
  try {
    const current = cfg.get();
    const sanitized = {};
    for (const key of ALLOWED_CONFIG_FIELDS) {
      if (req.body[key] !== undefined) sanitized[key] = req.body[key];
    }
    const merged = { ...current, ...sanitized };
    cfg.save(merged);
    res.json({ success: true, msg: 'Salvo. Reinicie o servidor para aplicar.' });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- Alternar Bot ---
app.post('/api/bot/toggle', auth, (req, res) => {
  try {
    const conf = cfg.get();
    conf.botEnabled = req.body.enabled !== undefined ? req.body.enabled : !conf.botEnabled;
    cfg.save(conf);
    res.json({ success: true, botEnabled: conf.botEnabled });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- Ferias ---
app.post('/api/vacation', auth, (req, res) => {
  try {
    const conf = cfg.get();
    if (req.body.enabled !== undefined) conf.vacationMode = req.body.enabled;
    if (req.body.message) conf.vacationMessage = req.body.message;
    cfg.save(conf);
    res.json({ success: true, vacationMode: conf.vacationMode });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- Webhook ---
app.get('/api/config/webhook', auth, (req, res) => res.json({ webhook: cfg.get().webhookUrl || null, secret: cfg.get().webhookSecret ? '***' : null }));
app.post('/api/config/webhook', auth, (req, res) => {
  try {
    const { url, secret } = req.body;
    if (!url) return res.status(400).json({ error: 'URL obrigatoria' });
    const conf = cfg.get();
    conf.webhookUrl = url;
    if (secret) conf.webhookSecret = secret;
    cfg.save(conf);
    res.json({ success: true, webhook: url });
  } catch (e) { res.status(500).json({ error: e.message }); }
});
app.delete('/api/config/webhook', auth, (req, res) => {
  try {
    const conf = cfg.get();
    conf.webhookUrl = null;
    conf.webhookSecret = null;
    cfg.save(conf);
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- Webhook Events Log ---
app.get('/api/webhook/events', auth, (req, res) => {
  try {
    const cid = req.query.client || '';
    const onlyFailed = req.query.failed === 'true';
    const events = onlyFailed ? db.getFailedWebhookEvents(cid) : db.getWebhookEvents(cid);
    res.json({ events });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/webhook/events/:id/replay', auth, async (req, res) => {
  try {
    const result = await core.replayWebhookEvent(req.params.id);
    res.json(result);
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- Configuracao de IA ---
app.get('/api/config/ai', auth, (req, res) => {
  const c = cfg.get();
  res.json({
    aiProvider: c.aiProvider || 'openai',
    openaiKey: c.openaiKey ? '***' : '',
    openaiModel: c.openaiModel || 'gpt-4o-mini',
    ollamaUrl: c.ollamaUrl || 'http://localhost:11434',
    ollamaModel: c.ollamaModel || 'llama3',
    hasOpenAI: !!c.openaiKey,
  });
});

app.put('/api/config/ai', auth, (req, res) => {
  try {
    const conf = cfg.get();
    const { aiProvider, openaiKey, openaiModel, ollamaUrl, ollamaModel } = req.body;
    if (aiProvider) conf.aiProvider = aiProvider;
    if (openaiKey !== undefined) conf.openaiKey = openaiKey || conf.openaiKey;
    if (openaiModel) conf.openaiModel = openaiModel;
    if (ollamaUrl) conf.ollamaUrl = ollamaUrl;
    if (ollamaModel) conf.ollamaModel = ollamaModel;
    cfg.save(conf);
    res.json({ success: true, msg: 'Configuracao de IA salva' });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- Visualizador de Log ---
app.get('/api/log', auth, (req, res) => {
  try {
    const logPath = path.join(__dirname, '..', 'server.log');
    if (!fs.existsSync(logPath)) return res.json({ lines: [] });
    const tail = Math.min(parseInt(req.query.lines) || 100, 5000);
    const stat = fs.statSync(logPath);
    const bufSize = Math.min(stat.size, tail * 200);
    const fd = fs.openSync(logPath, 'r');
    const buf = Buffer.alloc(bufSize);
    fs.readSync(fd, buf, 0, bufSize, Math.max(0, stat.size - bufSize));
    fs.closeSync(fd);
    const lines = buf.toString('utf-8').split('\n').filter(Boolean);
    res.json({ total: lines.length, lines: lines.slice(-tail) });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.delete('/api/log', auth, (req, res) => {
  try {
    const logPath = path.join(__dirname, '..', 'server.log');
    fs.writeFileSync(logPath, '', 'utf-8');
    res.json({ success: true, msg: 'Log limpo' });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- File Upload ---
app.post('/api/upload', auth, (req, res) => {
  try {
    const { fileName, data } = req.body;
    if (!fileName || !data) return res.status(400).json({ error: 'fileName e data (base64) obrigatorios' });
    const safeName = path.basename(fileName);
    const buf = Buffer.from(data, 'base64');
    const fp = path.join(wa.MEDIA_DIR, safeName);
    fs.writeFileSync(fp, buf);
    core.log('INFO', `Arquivo salvo: ${safeName}`);
    res.json({ success: true, fileName: safeName, size: buf.length });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- Backup & Restore ---
app.get('/api/backup', auth, (req, res) => {
  try {
    const dbPath = path.join(__dirname, '..', 'data', 'zap.db');
    if (!fs.existsSync(dbPath)) return res.status(404).json({ error: 'Banco nao encontrado' });
    const data = fs.readFileSync(dbPath);
    core.log('INFO', 'Backup do banco realizado');
    res.json({ success: true, size: data.length, data: data.toString('base64'), date: new Date().toISOString() });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/backup/restore', auth, (req, res) => {
  try {
    const { data } = req.body;
    if (!data) return res.status(400).json({ error: 'data (base64) obrigatorio' });
    const buf = Buffer.from(data, 'base64');
    const dbPath = path.join(__dirname, '..', 'data', 'zap.db');
    const backupPath = dbPath + '.bak.' + Date.now();
    if (fs.existsSync(dbPath)) fs.copyFileSync(dbPath, backupPath);
    fs.writeFileSync(dbPath, buf);
    db.reload();
    core.log('INFO', 'Banco restaurado com sucesso');
    res.json({ success: true, msg: 'Banco restaurado. Recarregue a pagina.' });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- Blacklist ---
app.get('/api/blacklist', auth, (req, res) => {
  try {
    const cid = req.query.client || '';
    if (cid) return res.json({ client: cid, blacklist: db.getBlacklist(cid) });
    const all = {};
    for (const c of wa.getAllClients()) all[c.id] = db.getBlacklist(c.id);
    res.json({ blacklist: all });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/blacklist', auth, (req, res) => {
  try {
    const { number, clientId = 'principal' } = req.body;
    if (!number) return res.status(400).json({ error: 'number obrigatorio' });
    db.addBlacklist(clientId, core.formatPhone(number));
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.delete('/api/blacklist', auth, (req, res) => {
  try {
    const { number, clientId = 'principal' } = req.body;
    if (!number) return res.status(400).json({ error: 'number obrigatorio' });
    db.removeBlacklist(clientId, core.formatPhone(number));
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- Modo Humano ---
app.get('/api/human', auth, (req, res) => {
  try {
    const cid = req.query.client || '';
    if (cid) return res.json({ client: cid, list: db.getHumanList(cid) });
    const all = {};
    for (const c of wa.getAllClients()) all[c.id] = db.getHumanList(c.id);
    res.json({ clients: all });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/human', auth, (req, res) => {
  try {
    const { number, action, clientId = 'principal' } = req.body;
    if (!number || !action) return res.status(400).json({ error: 'number e action (enable/disable) obrigatorios' });
    if (action === 'enable') { db.enableHuman(clientId, core.formatPhone(number)); res.json({ success: true }); }
    else if (action === 'disable') { db.disableHuman(clientId, core.formatPhone(number)); res.json({ success: true }); }
    else res.status(400).json({ error: 'acao invalida' });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- Templates ---
app.get('/api/templates', auth, (req, res) => {
  try { res.json({ templates: db.listTemplates() }); }
  catch (e) { res.status(500).json({ error: e.message }); }
});
app.post('/api/templates', auth, (req, res) => {
  try {
    const { name, content } = req.body;
    if (!name || !content) return res.status(400).json({ error: 'name e content obrigatorios' });
    db.addTemplate(name, content);
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});
app.delete('/api/templates', auth, (req, res) => {
  try {
    const { name } = req.body;
    if (!name) return res.status(400).json({ error: 'name obrigatorio' });
    db.deleteTemplate(name);
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});
app.delete('/api/templates/:name', auth, (req, res) => {
  try {
    db.deleteTemplate(req.params.name);
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- Agendamentos ---
app.get('/api/schedules', auth, (req, res) => {
  try { res.json({ schedules: db.listSchedules() }); }
  catch (e) { res.status(500).json({ error: e.message }); }
});
app.post('/api/schedules', auth, (req, res) => {
  try {
    const { name, phone, message, time, days, clientId = 'principal' } = req.body;
    if (!phone || !message || !time) return res.status(400).json({ error: 'phone, message e time (HH:MM) obrigatorios' });
    db.addSchedule({ id: Date.now().toString(36), name: name || 'Agendado', phone, message, time, days: days || [0,1,2,3,4,5,6], client_id: clientId, enabled: true, created_at: new Date().toISOString() });
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});
app.delete('/api/schedules', auth, (req, res) => {
  try {
    const { id } = req.body;
    if (!id) return res.status(400).json({ error: 'id obrigatorio' });
    db.deleteSchedule(id);
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});
app.patch('/api/schedules', auth, (req, res) => {
  try {
    const { id, enabled } = req.body;
    if (!id) return res.status(400).json({ error: 'id obrigatorio' });
    if (enabled !== undefined) db.toggleSchedule(id, enabled);
    else { const s = db.listSchedules().find(x => x.id === id); if (s) db.toggleSchedule(id, !s.enabled); }
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});
app.post('/api/schedules/:id/toggle', auth, (req, res) => {
  try {
    const s = db.listSchedules().find(x => x.id === req.params.id);
    if (!s) return res.status(404).json({ error: 'Agendamento nao encontrado' });
    db.toggleSchedule(req.params.id, !s.enabled);
    res.json({ success: true, enabled: !s.enabled });
  } catch (e) { res.status(500).json({ error: e.message }); }
});
app.delete('/api/schedules/:id', auth, (req, res) => {
  try {
    db.deleteSchedule(req.params.id);
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- Labels ---
app.get('/api/labels', auth, (req, res) => {
  try {
    const cid = req.query.client || '';
    const labels = db.getLabels(cid);
    const result = labels.map(l => ({ ...l, associations: db.getLabelAssociations(l.id) }));
    res.json({ labels: result });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/labels', auth, (req, res) => {
  try {
    const { name, color, clientId = 'principal' } = req.body;
    if (!name) return res.status(400).json({ error: 'name obrigatorio' });
    const id = `label_${Date.now()}`;
    db.addLabel({ id, client_id: clientId, name, color: color || '#3b82f6' });
    res.json({ success: true, id });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.put('/api/labels/:id', auth, (req, res) => {
  try {
    const { name, color } = req.body;
    if (!name) return res.status(400).json({ error: 'name obrigatorio' });
    const id = req.params.id;
    const cid = req.body.clientId || 'principal';
    db.addLabel({ id, client_id: cid, name, color: color || '#3b82f6' });
    core.emitWebhook(core.WEBHOOK_EVENTS.LABELS_EDIT, { id, name, color, clientId: cid }, cid);
    res.json({ success: true, id });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.delete('/api/labels/:id', auth, (req, res) => {
  try {
    db.removeLabel(req.params.id);
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/labels/associate', auth, (req, res) => {
  try {
    const { labelId, targetId } = req.body;
    if (!labelId || !targetId) return res.status(400).json({ error: 'labelId e targetId obrigatorios' });
    db.associateLabel(labelId, targetId);
    core.emitWebhook(core.WEBHOOK_EVENTS.LABELS_ASSOCIATION, { labelId, targetId }, req.body.clientId || 'principal');
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.delete('/api/labels/associate', auth, (req, res) => {
  try {
    const { labelId, targetId } = req.body;
    if (!labelId || !targetId) return res.status(400).json({ error: 'labelId e targetId obrigatorios' });
    db.removeLabelAssociation(labelId, targetId);
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- Contatos ---
app.get('/api/contacts', auth, async (req, res) => {
  try {
    const cid = req.query.client || 'principal';
    const waClient = wa.getWAClient(cid);
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });

    // Try to fetch fresh contacts from WhatsApp
    try {
      const contacts = await waClient.getContacts();
      for (const c of contacts) {
        if (c.id.user) {
          db.upsertContact({
            number: c.id.user, client_id: cid, name: c.name || c.pushname || null,
            pushname: c.pushname || null, is_blocked: c.isBlocked || false,
          });
        }
      }
    } catch {}

    const cached = db.getContacts(cid);
    res.json({ contacts: cached });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.get('/api/contacts/:number', auth, async (req, res) => {
  try {
    const cid = req.query.client || 'principal';
    const number = req.params.number;
    const waClient = wa.getWAClient(cid);
    if (waClient) {
      try {
        const contact = await waClient.getContactById(core.formatPhone(number));
        if (contact) {
          db.upsertContact({ number, client_id: cid, name: contact.name || contact.pushname, pushname: contact.pushname, is_blocked: contact.isBlocked });
        }
      } catch {}
    }
    const cached = db.getContact(number, cid);
    res.json({ contact: cached || { number } });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/contacts/block', auth, async (req, res) => {
  try {
    const waClient = wa.getWAClient(req.body.clientId || 'principal');
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const { number } = req.body;
    if (!number) return res.status(400).json({ error: 'number obrigatorio' });
    const contact = await waClient.getContactById(core.formatPhone(number));
    await contact.block();
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/contacts/unblock', auth, async (req, res) => {
  try {
    const waClient = wa.getWAClient(req.body.clientId || 'principal');
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const { number } = req.body;
    if (!number) return res.status(400).json({ error: 'number obrigatorio' });
    const contact = await waClient.getContactById(core.formatPhone(number));
    await contact.unblock();
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- Grupos ---
app.get('/api/groups', auth, async (req, res) => {
  try {
    const cid = req.query.client || 'principal';
    const waClient = wa.getWAClient(cid);
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const chats = await waClient.getChats();
    const groups = chats.filter(c => c.isGroup).map(g => ({
      id: g.id._serialized, name: g.name, unread: g.unreadCount, timestamp: g.timestamp,
      participants: g.participants?.length || 0,
    }));
    for (const g of groups) {
      db.upsertGroup({ id: g.id, client_id: cid, name: g.name, participants: g.participants });
    }
    res.json({ groups });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.get('/api/groups/:groupId', auth, async (req, res) => {
  try {
    const cid = req.query.client || 'principal';
    const waClient = wa.getWAClient(cid);
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const chat = await waClient.getChatById(req.params.groupId);
    if (!chat.isGroup) return res.status(400).json({ error: 'Nao e um grupo' });
    res.json({
      id: chat.id._serialized, name: chat.name, description: chat.description,
      participants: chat.participants?.map(p => ({ id: p.id._serialized, admin: p.isAdmin })),
      createdAt: chat.createdAt, unread: chat.unreadCount,
    });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/groups/create', auth, async (req, res) => {
  try {
    const waClient = wa.getWAClient(req.body.clientId || 'principal');
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const { name, participants } = req.body;
    if (!name || !participants?.length) return res.status(400).json({ error: 'name e participants obrigatorios' });
    const numbers = participants.map(n => core.formatPhone(n));
    const group = await waClient.createGroup(name, numbers);
    core.log('INFO', `Grupo criado: ${name}`);
    core.emitWebhook(core.WEBHOOK_EVENTS.GROUPS_UPSERT, { id: group.gid._serialized, name, participants: numbers }, req.body.clientId || 'principal');
    res.json({ success: true, id: group.gid._serialized });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/groups/leave', auth, async (req, res) => {
  try {
    const waClient = wa.getWAClient(req.body.clientId || 'principal');
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const { groupId } = req.body;
    const chat = await waClient.getChatById(groupId);
    await chat.leave();
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/groups/add', auth, async (req, res) => {
  try {
    const waClient = wa.getWAClient(req.body.clientId || 'principal');
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const { groupId, number } = req.body;
    if (!groupId || !number) return res.status(400).json({ error: 'groupId e number obrigatorios' });
    const chat = await waClient.getChatById(groupId);
    await chat.addParticipants([core.formatPhone(number)]);
    core.emitWebhook(core.WEBHOOK_EVENTS.GROUP_PARTICIPANTS_UPDATE, { groupId, action: 'add', participants: [number] }, req.body.clientId || 'principal');
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/groups/remove', auth, async (req, res) => {
  try {
    const waClient = wa.getWAClient(req.body.clientId || 'principal');
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const { groupId, number } = req.body;
    if (!groupId || !number) return res.status(400).json({ error: 'groupId e number obrigatorios' });
    const chat = await waClient.getChatById(groupId);
    await chat.removeParticipants([core.formatPhone(number)]);
    core.emitWebhook(core.WEBHOOK_EVENTS.GROUP_PARTICIPANTS_UPDATE, { groupId, action: 'remove', participants: [number] }, req.body.clientId || 'principal');
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/groups/promote', auth, async (req, res) => {
  try {
    const waClient = wa.getWAClient(req.body.clientId || 'principal');
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const { groupId, number } = req.body;
    const chat = await waClient.getChatById(groupId);
    await chat.promoteParticipants([core.formatPhone(number)]);
    core.emitWebhook(core.WEBHOOK_EVENTS.GROUP_PARTICIPANTS_UPDATE, { groupId, action: 'promote', participants: [number] }, req.body.clientId || 'principal');
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/groups/demote', auth, async (req, res) => {
  try {
    const waClient = wa.getWAClient(req.body.clientId || 'principal');
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const { groupId, number } = req.body;
    const chat = await waClient.getChatById(groupId);
    await chat.demoteParticipants([core.formatPhone(number)]);
    core.emitWebhook(core.WEBHOOK_EVENTS.GROUP_PARTICIPANTS_UPDATE, { groupId, action: 'demote', participants: [number] }, req.body.clientId || 'principal');
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/groups/setDescription', auth, async (req, res) => {
  try {
    const waClient = wa.getWAClient(req.body.clientId || 'principal');
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const { groupId, description } = req.body;
    const chat = await waClient.getChatById(groupId);
    await chat.setDescription(description);
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/groups/setSubject', auth, async (req, res) => {
  try {
    const waClient = wa.getWAClient(req.body.clientId || 'principal');
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const { groupId, subject } = req.body;
    const chat = await waClient.getChatById(groupId);
    await chat.setSubject(subject);
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/groups/send', auth, async (req, res) => {
  try {
    const waClient = wa.getWAClient(req.body.clientId || 'principal');
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const { groupId, message } = req.body;
    if (!groupId || !message) return res.status(400).json({ error: 'groupId e message obrigatorios' });
    await waClient.sendMessage(groupId, message);
    res.json({ success: true });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- Presenca ---
app.get('/api/presence/:number', auth, async (req, res) => {
  try {
    const cid = req.query.client || 'principal';
    const waClient = wa.getWAClient(cid);
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const contact = await waClient.getContactById(core.formatPhone(req.params.number));
    res.json({ number: req.params.number, isOnline: contact.isOnline, lastSeen: contact.lastSeen, isBlocked: contact.isBlocked });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/presence/sendPresence', auth, async (req, res) => {
  try {
    const waClient = wa.getWAClient(req.body.clientId || 'principal');
    if (!waClient) return res.status(503).json({ error: 'WhatsApp nao disponivel' });
    const { available } = req.body;
    await waClient.sendPresence(available !== false ? 'available' : 'unavailable');
    res.json({ success: true, status: available !== false ? 'available' : 'unavailable' });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- Chamadas ---
app.get('/api/calls', auth, (req, res) => {
  try {
    const cid = req.query.client || '';
    const calls = db.getCalls(cid);
    res.json({ calls });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// --- Midia ---
app.get('/api/media', auth, (req, res) => {
  try { const files = fs.readdirSync(wa.MEDIA_DIR); res.json({ count: files.length, files }); }
  catch (e) { res.status(500).json({ error: e.message }); }
});
app.get('/api/media/:fileName', auth, (req, res) => {
  const name = path.basename(req.params.fileName);
  const fp = path.join(wa.MEDIA_DIR, name);
  if (!fs.existsSync(fp)) return res.status(404).json({ error: 'Arquivo nao encontrado' });
  res.sendFile(fp);
});

// --- S3/MinIO ---
app.get('/api/s3/status', auth, (req, res) => {
  try { const s3 = require('./s3'); res.json({ enabled: s3.isEnabled() }); }
  catch { res.json({ enabled: false }); }
});

app.get('/api/s3/files', auth, async (req, res) => {
  try { const s3 = require('./s3'); const files = await s3.listFiles(); res.json({ files }); }
  catch (e) { res.status(500).json({ error: e.message }); }
});

// --- Proxy ---
app.put('/api/config/proxy', auth, (req, res) => {
  try {
    const conf = cfg.get();
    conf.proxy = req.body.proxy || null;
    cfg.save(conf);
    res.json({ success: true, proxy: conf.proxy });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.get('/api/config/proxy', auth, (req, res) => {
  res.json({ proxy: cfg.get().proxy || null });
});

// --- Config MQ ---
app.get('/api/config/mq', auth, (req, res) => {
  const mq = cfg.get().messageQueue || {};
  res.json({
    rabbitmq: mq.rabbitmq ? '***' : null,
    kafka: mq.kafka?.brokers ? '***' : null,
    sqs: mq.sqs?.queueUrl ? '***' : null,
    enabled: !!(mq.rabbitmq || mq.kafka || mq.sqs),
  });
});

// --- Controle do Servidor ---
let serverOpLock = false;
app.post('/api/server/stop', auth, (req, res) => {
  if (serverOpLock) return res.status(429).json({ error: 'Operacao ja em andamento' });
  serverOpLock = true;
  try {
    core.log('INFO', 'Servidor parando pelo web admin...');
    res.json({ success: true, msg: 'Servidor sera desligado em 1s' });
    setTimeout(() => { stop(() => process.exit(0)); }, 1000);
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.post('/api/server/restart', auth, (req, res) => {
  if (serverOpLock) return res.status(429).json({ error: 'Operacao ja em andamento' });
  serverOpLock = true;
  try {
    core.log('INFO', 'Servidor reiniciando pelo web admin...');
    res.json({ success: true, msg: 'Servidor sera reiniciado (se estiver sob PM2/process manager)' });
    setTimeout(() => { stop(() => process.exit(0)); }, 1000);
  } catch (e) { res.status(500).json({ error: e.message }); }
});

app.get('/api/server/status', auth, (req, res) => {
  res.json({
    running: true,
    pid: process.pid,
    uptime: Math.floor((Date.now() - global.START_TIME) / 1000),
    uptimeFormatted: core.fmtUptime(Math.floor((Date.now() - global.START_TIME) / 1000)),
    startTime: new Date(global.START_TIME).toISOString(),
    nodeVersion: process.version,
    platform: process.platform,
    memory: process.memoryUsage(),
  });
});

// --- Estatisticas ---
app.get('/api/stats', auth, (req, res) => {
  try {
    const conf = cfg.get();
    const allStats = db.getAllStats();
    const clients = wa.getAllClients().map(c => {
      const s = allStats.find(x => x.client_id === c.id) || {};
      return { id: c.id, connected: c.connected, ready: c.ready, received: s.received || 0, sent: s.sent || 0, media: s.media || 0, storedMsgs: db.countMessages(c.id), humanCount: db.getHumanList(c.id).length, blacklistCount: db.getBlacklist(c.id).length };
    });
    const totals = clients.reduce((a, c) => ({ received: a.received + c.received, sent: a.sent + c.sent, media: a.media + c.media }), { received: 0, sent: 0, media: 0 });
    res.json({
      uptime: Math.floor((Date.now() - global.START_TIME) / 1000),
      uptimeFormatted: core.fmtUptime(Math.floor((Date.now() - global.START_TIME) / 1000)),
      startTime: new Date(global.START_TIME).toISOString(), mode: conf.mode,
      botEnabled: conf.botEnabled, vacationMode: conf.vacationMode, aiProvider: conf.aiProvider || 'openai', ai: !!(conf.openaiKey || conf.aiProvider === 'ollama'),
      mediaFiles: fs.readdirSync(wa.MEDIA_DIR).length, clients, totals,
    });
  } catch (e) { res.status(500).json({ error: e.message }); }
});

// ================================================================
// AGENDADOR
// ================================================================
let SCHEDULER = null;
function startScheduler() {
  if (SCHEDULER) clearInterval(SCHEDULER);
  SCHEDULER = setInterval(() => {
    try {
      const now = new Date();
      const pad = n => String(n).padStart(2, '0');
      const key = `${pad(now.getHours())}:${pad(now.getMinutes())}`;
      const today = now.getDay();
      for (const s of db.listSchedules()) {
        if (!s.enabled) continue;
        const days = JSON.parse(s.days || '[0,1,2,3,4,5,6]');
        if (!days.includes(today) || s.time !== key) continue;
        const waClient = wa.getWAClient(s.client_id || 'principal');
        if (!waClient) { core.log('WARN', `Schedule: cliente ${s.client_id} nao disponivel`); continue; }
        waClient.sendMessage(core.formatPhone(s.phone), s.message).then(() => {
          core.log('INFO', `Agendamento "${s.name}" executado`, s.client_id);
        }).catch(e => core.log('ERROR', `Agendamento "${s.name}" falhou: ${e.message}`, s.client_id));
      }
    } catch (e) { core.log('ERROR', `Scheduler: ${e.message}`); }
  }, 30000);
  core.log('INFO', 'Scheduler iniciado');
}

function stopScheduler() {
  if (SCHEDULER) { clearInterval(SCHEDULER); SCHEDULER = null; }
}

// ================================================================
// INICIO
// ================================================================
function start(port) {
  global.START_TIME = Date.now();
  startScheduler();
  return new Promise((resolve) => {
    const onError = (err) => {
      if (err.code === 'EADDRINUSE') {
        core.log('ERROR', `Porta ${port} ja esta em uso. Outra instancia da SoenWebApi esta rodando?`);
        console.error(`\n[SoenWebApi] PORTA ${port} EM USO.`);
        console.error('[SoenWebApi] Se outra instancia ja esta rodando, nao inicie outra.');
        process.exit(1);
      }
      throw err;
    };
    server.once('error', onError);
    // Escuta somente em localhost: o servico e local e nao deve ficar
    // acessivel por outros dispositivos da rede.
    server.listen(port, '127.0.0.1', () => {
      server.removeListener('error', onError);
      resolve(server);
    });
  });
}

function stop(callback) {
  stopScheduler();
  clearInterval(WS_PING_INTERVAL);
  try { wss.close(); } catch {}
  for (const ws of wsClients) { try { ws.terminate(); } catch {} }
  wsClients.clear();
  wa.destroyAll();
  db.close();
  core.closeLog();
  server.close(callback);
}

module.exports = { app, server, start, stop, wss, stopScheduler };
