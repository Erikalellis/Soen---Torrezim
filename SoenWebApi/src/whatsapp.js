const { Client, LocalAuth, MessageMedia } = require('whatsapp-web.js');
const QR = require('qrcode');
const axios = require('axios');
const fs = require('fs');
const path = require('path');
const cfg = require('./config');
const db = require('./database');
const core = require('./core');

const MEDIA_DIR = path.join(__dirname, '..', 'media');
if (!fs.existsSync(MEDIA_DIR)) fs.mkdirSync(MEDIA_DIR, { recursive: true });

const clients = {};

function buildPuppeteerOpts(clientCfg) {
  // Janela fora da tela: o Chromium cria uma janela invisivel (fantasma) no
  // canto da tela no Windows mesmo em modo headless; forca o posicionamento
  // para fora da area visivel, eliminando o "quadrado fantasma".
  const opts = { args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage', '--window-position=-32000,-32000'] };
  const conf = cfg.get();
  let cp = clientCfg.chromiumPath || conf.chromiumPath || '';
  if (!cp || !fs.existsSync(cp)) {
    const paths = [
      path.join(__dirname, '..', 'ChromiumPortable', 'App', 'Chromium', 'chrome.exe'),
      'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
      'C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe',
      path.join(__dirname, '..', 'ChromiumPortable', 'App', 'Chromium', '64', 'chrome.exe'),
    ];
    cp = paths.find(p => fs.existsSync(p)) || '';
  }
  if (cp && fs.existsSync(cp)) opts.executablePath = cp;

  // Proxy per-instancia
  const proxy = clientCfg.proxy || conf.proxy;
  if (proxy) {
    opts.args.push(`--proxy-server=${proxy}`);
    core.log('INFO', `Proxy configurado: ${proxy}`, clientCfg.id || 'principal');
  }
  return opts;
}

const SYSTEM_PROMPT = (botName) => `Você é o assistente virtual da empresa "${botName}". Responda de forma educada, direta e profissional. Mantenha respostas curtas (max 3 parágrafos). Use emojis moderadamente.`;

async function callOpenAI(prompt, botName, conf) {
  const key = conf.openaiKey || process.env.OPENAI_API_KEY;
  if (!key) return null;
  const model = conf.openaiModel || 'gpt-4o-mini';
  try {
    const r = await axios.post('https://api.openai.com/v1/chat/completions', {
      model, messages: [
        { role: 'system', content: SYSTEM_PROMPT(botName) },
        { role: 'user', content: prompt },
      ], max_tokens: 200, temperature: 0.7,
    }, { headers: { Authorization: `Bearer ${key}`, 'Content-Type': 'application/json' }, timeout: 15000 });
    return r.data.choices[0].message.content.trim();
  } catch (e) {
    core.log('WARN', `OpenAI: ${e.message}`);
    return null;
  }
}

async function callOllama(prompt, botName, conf) {
  const url = (conf.ollamaUrl || 'http://localhost:11434').replace(/\/+$/, '') + '/api/chat';
  const model = conf.ollamaModel || 'llama3';
  try {
    const r = await axios.post(url, {
      model, stream: false,
      messages: [
        { role: 'system', content: SYSTEM_PROMPT(botName) },
        { role: 'user', content: prompt },
      ],
      options: { num_predict: 200, temperature: 0.7 },
    }, { timeout: 30000 });
    return r.data.message?.content?.trim() || null;
  } catch (e) {
    core.log('WARN', `Ollama: ${e.message}`);
    return null;
  }
}

async function callAI(prompt, botName) {
  const conf = cfg.get();
  const provider = (conf.aiProvider || 'openai').toLowerCase();
  if (provider === 'ollama') return callOllama(prompt, botName, conf);
  return callOpenAI(prompt, botName, conf);
}

// --- Transcricao de audio ---
async function transcribeAudio(audioPath) {
  try { const t = require('./transcribe'); return await t.transcribeAudio(audioPath); } catch { return null; }
}

// --- Processamento de midia ---
async function processMedia(filePath) {
  const sharp = require('sharp');
  const ext = path.extname(filePath).toLowerCase();
  if (['.jpg', '.jpeg', '.png', '.webp'].includes(ext)) {
    const outPath = filePath.replace(/(\.\w+)$/, '_thumb$1');
    try {
      await sharp(filePath).resize(320, 320, { fit: 'inside' }).toFile(outPath);
      return outPath;
    } catch {}
  }
  return filePath;
}

async function processMessage(msg, waClient, clientCfg, cid) {
  if (!msg.from || msg.from.endsWith('@g.us') || msg.type !== 'chat') return;

  const raw = msg.body;
  const norm = core.normalize(raw);
  const num = msg.from;
  const safeNum = num.replace('@c.us', '');
  const conf = cfg.get();
  const botName = clientCfg.name || conf.botName || 'Bot';

  if (conf.vacationMode && conf.vacationMessage) {
    await waClient.sendMessage(num, `🧉 ${conf.vacationMessage}`);
    return;
  }
  if (!conf.botEnabled) return;

  db.incrementStat(cid, 'received');

  if (db.isBlacklisted(cid, num)) return;
  if (db.isHuman(cid, num)) return;

  if (conf.controllerNumber) {
    if (num !== core.formatPhone(conf.controllerNumber)) {
      await waClient.sendMessage(num, '📱 Modo administrador ativo. Apenas o administrador pode interagir.');
      return;
    }
  }

  const md = {
    id: msg.id.id, client_id: cid, from: safeNum, body: raw,
    has_media: msg.hasMedia, timestamp: msg.timestamp,
  };

  if (msg.hasMedia) {
    try {
      const media = await msg.downloadMedia();
      const ext = (media.mimetype || 'application/octet-stream').split('/')[1] || 'bin';
      const fn = `${msg.id.id}.${ext}`;
      const fp = path.join(MEDIA_DIR, fn);
      fs.writeFileSync(fp, media.data, 'base64');
      md.media_file = fn;
      db.incrementStat(cid, 'media');

      // Transcricao automatica de audio
      if (media.mimetype?.startsWith('audio/')) {
        const text = await transcribeAudio(fp);
        if (text) {
          md.transcribed = text;
          core.log('INFO', `Audio transcrito de ${safeNum}: "${text.substring(0, 60)}..."`, cid);
        }
      }

      // Upload para S3
      try { const s3 = require('./s3'); await s3.uploadFile(fp, fn); } catch {}
    } catch {}
  }
  db.insertMessage(md);

  core.broadcast('message', { ...md, client_id: cid });
  core.emitWebhook(core.WEBHOOK_EVENTS.MESSAGES_UPSERT, { ...md, raw }, cid);

  const greetings = clientCfg.greetings || ['oi', 'ola', 'bom dia', 'boa tarde', 'boa noite', 'menu', 'iniciar'];
  const greetingRegex = new RegExp(`^(${greetings.map(g => g.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')).join('|')})[\\s!.,]*$`, 'i');

  if (greetingRegex.test(raw.trim())) {
    const menu = (clientCfg.greetingMessage || '🤖 *[NOME]*\n\nDigite *0* para o menu.').replace(/\[NOME\]/g, botName);
    await waClient.sendMessage(num, menu);
    return;
  }

  let matched = false;
  for (const intent of (clientCfg.intents || [])) {
    if (intent.patterns.some(p => new RegExp('\\b' + p.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '\\b', 'i').test(norm))) {
      const resp = intent.response.replace(/\[NOME\]/g, botName);
      if (intent.redirect) {
        const redirects = conf.redirects || {};
        let redirected = false;
        for (const [rk, rv] of Object.entries(redirects)) {
          if (rv.phone && new RegExp('\\b' + rk.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '\\b', 'i').test(norm)) {
            const target = core.formatPhone(rv.phone);
            await waClient.sendMessage(num, `🧑‍💼 Transferindo para *${rv.label || rk}*...`);
            try {
              const contact = await waClient.getContactById(num);
              const name = contact.pushname || contact.name || safeNum;
              await waClient.sendMessage(target, `📞 *${rv.label || rk}* - Cliente *${name}* (${safeNum}):\n\n"${raw}"`);
            } catch {}
            core.log('INFO', `${safeNum} redirecionado para ${rk} (${rv.phone})`, cid);
            redirected = true;
            break;
          }
        }
        if (!redirected) {
          await msg.reply(resp);
          db.enableHuman(cid, num);
          const resetMs = (conf.humanResetHours || 24) * 60 * 60 * 1000;
          setTimeout(() => { db.disableHuman(cid, num); }, resetMs);
          core.log('INFO', `${safeNum} -> modo humano`, cid);
        }
      } else {
        await msg.reply(resp);
      }
      matched = true;
      break;
    }
  }

  if (!matched) {
    const aiResp = await callAI(raw, botName);
    if (aiResp) {
      await msg.reply(aiResp);
    } else {
      await msg.reply((clientCfg.fallbackMessage || '🤖 Nao entendi. Digite *MENU*.').replace(/\[NOME\]/g, botName));
    }
  }
}

function initClient(clientCfg) {
  const cid = clientCfg.id || 'principal';
  let sessionDir = path.join(__dirname, '..', clientCfg.sessionDir || `sessions/${cid}`);
  sessionDir = path.resolve(sessionDir);
  const projectRoot = path.resolve(__dirname, '..');
  if (!sessionDir.startsWith(projectRoot)) sessionDir = path.join(projectRoot, 'sessions', cid);
  db.initStats(cid);

  clients[cid] = { client: null, config: clientCfg, qrCode: null, connected: false, ready: false, _reconnect: 0 };

  try {
    const waClient = new Client({
      authStrategy: new LocalAuth({ dataPath: sessionDir }),
      puppeteer: buildPuppeteerOpts(clientCfg),
    });

    clients[cid].client = waClient;

    // --- QR Code ---
    waClient.on('qr', (qr) => {
      try {
        clients[cid].connected = false;
        clients[cid].ready = false;
        if (!cfg.get().apiKey) QR.toString(qr, { type: 'terminal', small: true }, (e, s) => { if (!e) console.log(s); });
        QR.toDataURL(qr, { width: 400, margin: 1 }, (e, url) => {
          try {
            if (e) return;
            clients[cid].qrCode = url;
            core.broadcast('qr', { client_id: cid, qr: url });
            core.emitWebhook(core.WEBHOOK_EVENTS.QRCODE_UPDATED, { qr: url }, cid);
          } catch (innerErr) { core.log('ERROR', `QR callback: ${innerErr.message}`, cid); }
        });
        core.log('INFO', `QR gerado para "${clientCfg.name || cid}"`, cid);
      } catch (err) { core.log('ERROR', `QR handler: ${err.message}`, cid); }
    });

    waClient.on('authenticated', () => { clients[cid].qrCode = null; });
    waClient.on('auth_failure', () => { clients[cid].qrCode = null; });

    // --- Ready ---
    waClient.on('ready', () => {
      clients[cid].connected = true;
      clients[cid].ready = true;
      clients[cid].qrCode = null;
      clients[cid]._reconnect = 0;
      core.log('INFO', `"${clientCfg.name || cid}" conectado!`, cid);
      core.broadcast('status', { client_id: cid, connected: true, ready: true });
      core.emitWebhook(core.WEBHOOK_EVENTS.CONNECTION_UPDATE, { connected: true, ready: true }, cid);
    });

    // --- Disconnected ---
    waClient.on('disconnected', (reason) => {
      clients[cid].connected = false;
      clients[cid].ready = false;
      core.log('WARN', `"${clientCfg.name || cid}" desconectado: ${reason}`, cid);
      core.broadcast('status', { client_id: cid, connected: false, ready: false });
      core.emitWebhook(core.WEBHOOK_EVENTS.CONNECTION_UPDATE, { connected: false, reason }, cid);
      const attempt = (clients[cid]._reconnect || 0) + 1;
      clients[cid]._reconnect = attempt;
      const delay = Math.min(60000, 5000 * attempt);
      core.log('INFO', `Reconectando em ${delay/1000}s (tentativa ${attempt})`, cid);
      clearTimeout(clients[cid]._reconnectTimer);
      clients[cid]._reconnectTimer = setTimeout(() => {
        try { waClient.initialize(); } catch (e) { core.log('ERROR', `Reconnect: ${e.message}`, cid); }
      }, delay);
    });

    // --- Message received ---
    waClient.on('message', async (msg) => {
      try { await processMessage(msg, waClient, clientCfg, cid); }
      catch (e) { core.log('ERROR', `Handler: ${e.message}`, cid); }
    });

    // --- Message ack (update) ---
    waClient.on('message_ack', (msg, ack) => {
      core.emitWebhook(core.WEBHOOK_EVENTS.MESSAGES_UPDATE, {
        id: msg.id.id, from: msg.from, body: msg.body, ack,
      }, cid);
    });

    // --- Presence update ---
    waClient.on('presence_changed', (presence) => {
      core.emitWebhook(core.WEBHOOK_EVENTS.PRESENCE_UPDATE, {
        id: presence.id, chatId: presence.chatId, state: presence.state,
        isOnline: presence.isOnline, lastSeen: presence.lastSeen,
      }, cid);
    });

    // --- Group notification ---
    waClient.on('group_join', (notification) => {
      core.emitWebhook(core.WEBHOOK_EVENTS.GROUPS_UPSERT, {
        type: 'join', groupId: notification.chatId,
        author: notification.author, recipientIds: notification.recipientIds,
      }, cid);
    });
    waClient.on('group_leave', (notification) => {
      core.emitWebhook(core.WEBHOOK_EVENTS.GROUPS_UPDATE, {
        type: 'leave', groupId: notification.chatId,
        author: notification.author, recipientIds: notification.recipientIds,
      }, cid);
    });
    waClient.on('group_update', (notification) => {
      core.emitWebhook(core.WEBHOOK_EVENTS.GROUPS_UPDATE, {
        groupId: notification.chatId, type: notification.type,
      }, cid);
    });

    // Call handling
    waClient.on('call', async (call) => {
      core.log('INFO', `Chamada recebida de ${call.from} (${call.isVideo ? 'video' : 'audio'})`, cid);
      db.insertCall({
        id: call.id, client_id: cid, from_number: call.from.replace('@c.us', ''),
        status: 'received', timestamp: Date.now(),
      });
      core.emitWebhook(core.WEBHOOK_EVENTS.CALL_UPSERT, {
        id: call.id, from: call.from, isVideo: call.isVideo, status: 'received',
      }, cid);
      try { await call.reject(); core.log('INFO', `Chamada rejeitada de ${call.from}`, cid); } catch {}
    });

    // --- Contacts events ---
    waClient.on('contacts_set', (contacts) => {
      core.emitWebhook(core.WEBHOOK_EVENTS.CONTACTS_SET, { count: contacts.length }, cid);
    });
    waClient.on('contacts_upsert', (contacts) => {
      core.emitWebhook(core.WEBHOOK_EVENTS.CONTACTS_UPSERT, { contacts: contacts.map(c => ({ id: c.id.user, name: c.name, pushname: c.pushname })) }, cid);
    });
    waClient.on('contacts_update', (contact) => {
      core.emitWebhook(core.WEBHOOK_EVENTS.CONTACTS_UPDATE, { id: contact.id.user, name: contact.name, pushname: contact.pushname, isBlocked: contact.isBlocked }, cid);
    });

    // --- Chats events ---
    waClient.on('chats_set', (chats) => {
      core.emitWebhook(core.WEBHOOK_EVENTS.CHATS_SET, { count: chats.length }, cid);
    });
    waClient.on('chats_upsert', (chat) => {
      core.emitWebhook(core.WEBHOOK_EVENTS.CHATS_UPSERT, { id: chat.id._serialized, name: chat.name, isGroup: chat.isGroup, unreadCount: chat.unreadCount }, cid);
    });
    waClient.on('chats_update', (chat) => {
      core.emitWebhook(core.WEBHOOK_EVENTS.CHATS_UPDATE, { id: chat.id._serialized, name: chat.name, unreadCount: chat.unreadCount }, cid);
    });
    waClient.on('chats_delete', (chat) => {
      core.emitWebhook(core.WEBHOOK_EVENTS.CHATS_DELETE, { id: chat.id._serialized }, cid);
    });

    waClient.initialize().catch(e => core.log('ERROR', `Init "${clientCfg.name || cid}": ${e.message}`, cid));
    core.log('INFO', `Iniciando cliente "${clientCfg.name || cid}"`, cid);
  } catch (e) {
    core.log('ERROR', `Falha ao criar cliente "${clientCfg.name || cid}": ${e.message}`, cid);
  }
}

function getClient(cid) { return clients[cid] || null; }

function getAllClients() { return Object.entries(clients).map(([id, c]) => ({ id, connected: c.connected, ready: c.ready, qrCode: c.qrCode, config: c.config })); }

function getWAClient(cid) { const c = clients[cid]; return c ? c.client : null; }

function destroyAll() {
  for (const c of Object.values(clients)) try { c.client.destroy(); } catch {}
}

module.exports = { initClient, getClient, getAllClients, getWAClient, destroyAll, MEDIA_DIR, MessageMedia, callAI, processMedia, transcribeAudio };
