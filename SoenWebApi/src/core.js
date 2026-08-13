const fs = require('fs');
const path = require('path');
const axios = require('axios');
const crypto = require('crypto');
const LOG_PATH = path.join(__dirname, '..', 'server.log');

let logStream = null;
const LOG_MAX_SIZE = 5 * 1024 * 1024;

function init() {
  try {
    const stat = fs.statSync(LOG_PATH);
    if (stat.size > LOG_MAX_SIZE) {
      const backup = LOG_PATH + '.' + new Date().toISOString().slice(0, 10);
      fs.renameSync(LOG_PATH, backup);
    }
  } catch {}
  logStream = fs.createWriteStream(LOG_PATH, { flags: 'a' });
}

function log(level, msg, clientId) {
  const ts = new Date().toISOString();
  const tag = clientId ? `[${clientId}]` : '[SYS]';
  const line = `${ts} ${tag} [${level}] ${msg}`;
  if (logStream) logStream.write(line + '\n');
  console.log(line);
}

function normalize(str) {
  return str.toLowerCase().trim().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[^a-z0-9\s]/g, '');
}

function formatPhone(num) {
  return num.includes('@c.us') ? num : `${num.replace(/[^0-9]/g, '')}@c.us`;
}

function fmtUptime(secs) {
  const h = Math.floor(secs / 3600), m = Math.floor((secs % 3600) / 60), s = secs % 60;
  return h ? `${h}h ${m}m ${s}s` : m ? `${m}m ${s}s` : `${s}s`;
}

function validate(fields) {
  const errors = [];
  for (const [key, rules] of Object.entries(fields)) {
    const val = rules.value;
    if (rules.required && (val === undefined || val === null || val === '')) errors.push(`${key} é obrigatório`);
    if (val !== undefined && val !== null && val !== '') {
      if (rules.type === 'string' && typeof val !== 'string') errors.push(`${key} deve ser texto`);
      if (rules.type === 'number' && isNaN(Number(val))) errors.push(`${key} deve ser número`);
      if (rules.minLength && String(val).length < rules.minLength) errors.push(`${key} deve ter no mínimo ${rules.minLength} caracteres`);
      if (rules.maxLength && String(val).length > rules.maxLength) errors.push(`${key} deve ter no máximo ${rules.maxLength} caracteres`);
      if (rules.pattern && !rules.pattern.test(String(val))) errors.push(`${key} está em formato inválido`);
      if (rules.type === 'array' && !Array.isArray(val)) errors.push(`${key} deve ser uma lista`);
      if (rules.type === 'array' && Array.isArray(val) && val.length < (rules.minItems || 0)) errors.push(`${key} deve ter pelo menos ${rules.minItems} item(ns)`);
    }
  }
  return errors;
}

// --- Webhook Event System ---
const WEBHOOK_EVENTS = {
  QRCODE_UPDATED: 'qrcode.updated',
  CONNECTION_UPDATE: 'connection.update',
  MESSAGES_SET: 'messages.set',
  MESSAGES_UPSERT: 'messages.upsert',
  MESSAGES_UPDATE: 'messages.update',
  SEND_MESSAGE: 'send.message',
  CONTACTS_SET: 'contacts.set',
  CONTACTS_UPSERT: 'contacts.upsert',
  CONTACTS_UPDATE: 'contacts.update',
  PRESENCE_UPDATE: 'presence.update',
  CHATS_SET: 'chats.set',
  CHATS_UPDATE: 'chats.update',
  CHATS_UPSERT: 'chats.upsert',
  CHATS_DELETE: 'chats.delete',
  GROUPS_UPSERT: 'groups.upsert',
  GROUPS_UPDATE: 'groups.update',
  GROUP_PARTICIPANTS_UPDATE: 'group-participants.update',
  CALL_UPSERT: 'call.upsert',
  LABELS_ASSOCIATION: 'labels.association',
  LABELS_EDIT: 'labels.edit',
};

async function sendWebhook(url, payload, headers, event, clientId) {
  for (let attempt = 0; attempt < 3; attempt++) {
    try {
      await axios.post(url, payload, { headers, timeout: 5000 });
      return { success: true };
    } catch (e) {
      if (attempt < 2) {
        await new Promise(r => setTimeout(r, (attempt + 1) * 2000));
      } else {
        return { success: false, error: e.message };
      }
    }
  }
}

async function emitWebhook(event, data, clientId) {
  const db = require('./database');
  const cfg = require('./config');
  const conf = cfg.get();

  // Broadcast via WebSocket
  try {
    broadcast(event, { client_id: clientId, ...data });
  } catch {}

  // Collect all webhook endpoints
  const webhooks = [];
  const url = conf.webhookUrl;
  const secret = conf.webhookSecret || process.env.WEBHOOK_SECRET;
  if (url) webhooks.push({ url, secret, events: null });
  if (Array.isArray(conf.webhooks)) {
    for (const wh of conf.webhooks) {
      webhooks.push({ url: wh.url, secret: wh.secret, events: wh.events || null });
    }
  }

  if (webhooks.length === 0) return;

  const payload = JSON.stringify({ event, data, client_id: clientId, timestamp: Date.now() });
  const results = [];

  for (const wh of webhooks) {
    // Skip if webhook filters events and this event is not in its list
    if (Array.isArray(wh.events) && !wh.events.includes(event)) continue;

    const headers = { 'Content-Type': 'application/json' };
    if (wh.secret) {
      const signature = crypto.createHmac('sha256', wh.secret).update(payload).digest('hex');
      headers['X-Webhook-Signature'] = signature;
    }
    const result = await sendWebhook(wh.url, payload, headers, event, clientId);
    results.push(result);
  }

  // Log event with status (failed if ANY webhook failed)
  const allFailed = results.every(r => !r.success);
  try {
    db.logWebhookEvent(clientId || 'system', event, data);
    if (allFailed) {
      const errMsg = results.filter(r => !r.success).map(r => r.error).join('; ');
      db.updateWebhookEventStatus(db.getWebhookEvents(clientId || 'system', 1)[0]?.id, 'failed', errMsg);
    }
  } catch {}

  // Publish to MQ
  try { const mq = require('./mq'); await mq.publishEvent(event, { client_id: clientId, ...data }); } catch {}
}

async function replayWebhookEvent(eventId) {
  const db = require('./database');
  const cfg = require('./config');
  const conf = cfg.get();
  const evt = db.getWebhookEvents(conf.clients?.[0]?.id || '', 999).find(e => e.id == eventId);
  if (!evt) return { success: false, error: 'Evento nao encontrado' };

  const payload = JSON.stringify({ event: evt.event, data: JSON.parse(evt.payload), client_id: evt.client_id, timestamp: Date.now() });

  const webhooks = [];
  const url = conf.webhookUrl;
  const secret = conf.webhookSecret || process.env.WEBHOOK_SECRET;
  if (url) webhooks.push({ url, secret, events: null });
  if (Array.isArray(conf.webhooks)) {
    for (const wh of conf.webhooks) webhooks.push({ url: wh.url, secret: wh.secret, events: wh.events || null });
  }

  const results = [];
  for (const wh of webhooks) {
    if (Array.isArray(wh.events) && !wh.events.includes(evt.event)) {
      results.push({ url: wh.url, success: true, skipped: true });
      continue;
    }
    const headers = { 'Content-Type': 'application/json' };
    if (wh.secret) {
      const signature = crypto.createHmac('sha256', wh.secret).update(payload).digest('hex');
      headers['X-Webhook-Signature'] = signature;
    }
    const r = await sendWebhook(wh.url, payload, headers, evt.event, evt.client_id);
    results.push({ url: wh.url, ...r });
  }

  const allOk = results.every(r => r.success !== false);
  db.updateWebhookEventStatus(evt.id, allOk ? 'sent' : 'failed', allOk ? null : results.filter(r => r.success === false).map(r => r.error).join('; '));
  return { success: allOk, results };
}

function checkLogRotation() {
  try {
    const stat = fs.statSync(LOG_PATH);
    if (stat.size > LOG_MAX_SIZE) {
      if (logStream) logStream.end();
      const backup = LOG_PATH + '.' + new Date().toISOString().slice(0, 10);
      fs.renameSync(LOG_PATH, backup);
      logStream = fs.createWriteStream(LOG_PATH, { flags: 'a' });
    }
  } catch {}
}

function closeLog() {
  if (logStream) { try { logStream.end(); } catch {} logStream = null; }
}

// WebSocket broadcast
let wsBroadcast = null;
function setBroadcast(fn) { wsBroadcast = fn; }
function broadcast(event, data) { if (wsBroadcast) wsBroadcast(event, data); }

module.exports = { init, log, closeLog, normalize, formatPhone, fmtUptime, validate, setBroadcast, broadcast, emitWebhook, replayWebhookEvent, WEBHOOK_EVENTS, LOG_PATH, checkLogRotation };
