const path = require('path');
const fs = require('fs');

const DB_PATH = process.env.DB_PATH || path.join(__dirname, '..', 'data', 'zap.db');
const LOG_PATH = path.join(__dirname, '..', 'server.log');
const logStream = fs.createWriteStream(LOG_PATH, { flags: 'a' });

let db = null;
let DB_INITED = false;

function open() {
  const Database = require('better-sqlite3');
  const dir = path.dirname(DB_PATH);
  if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
  db = new Database(DB_PATH);
  db.pragma('journal_mode = WAL');
  db.pragma('foreign_keys = ON');
  migrate();
  DB_INITED = true;
  return db;
}

function migrate() {
  db.exec(`
    CREATE TABLE IF NOT EXISTS messages (
      id TEXT PRIMARY KEY, client_id TEXT, from_number TEXT,
      body TEXT, has_media INTEGER DEFAULT 0, media_file TEXT,
      timestamp INTEGER, stored_at INTEGER
    );
    CREATE INDEX IF NOT EXISTS idx_msgs_client ON messages(client_id);
    CREATE INDEX IF NOT EXISTS idx_msgs_ts ON messages(timestamp DESC);

    CREATE TABLE IF NOT EXISTS stats (
      client_id TEXT PRIMARY KEY, received INTEGER DEFAULT 0,
      sent INTEGER DEFAULT 0, media INTEGER DEFAULT 0
    );

    CREATE TABLE IF NOT EXISTS blacklist (
      client_id TEXT, number TEXT, PRIMARY KEY (client_id, number)
    );

    CREATE TABLE IF NOT EXISTS human_mode (
      client_id TEXT, number TEXT, since INTEGER, PRIMARY KEY (client_id, number)
    );

    CREATE TABLE IF NOT EXISTS schedules (
      id TEXT PRIMARY KEY, name TEXT, phone TEXT, message TEXT,
      time TEXT, days TEXT, client_id TEXT, enabled INTEGER DEFAULT 1,
      created_at TEXT
    );

    CREATE TABLE IF NOT EXISTS templates (
      name TEXT PRIMARY KEY, content TEXT, created_at TEXT
    );

    CREATE TABLE IF NOT EXISTS config_store (
      key TEXT PRIMARY KEY, value TEXT
    );

    CREATE TABLE IF NOT EXISTS labels (
      id TEXT PRIMARY KEY, client_id TEXT, name TEXT, color TEXT DEFAULT '#3b82f6',
      created_at INTEGER
    );

    CREATE TABLE IF NOT EXISTS label_associations (
      label_id TEXT, target_id TEXT, target_type TEXT DEFAULT 'chat',
      PRIMARY KEY (label_id, target_id),
      FOREIGN KEY (label_id) REFERENCES labels(id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS contacts_cache (
      number TEXT, client_id TEXT, name TEXT, pushname TEXT,
      profile_pic TEXT, is_blocked INTEGER DEFAULT 0,
      updated_at INTEGER, PRIMARY KEY (number, client_id)
    );

    CREATE TABLE IF NOT EXISTS groups_cache (
      id TEXT PRIMARY KEY, client_id TEXT, name TEXT,
      description TEXT, participants INTEGER DEFAULT 0,
      created_at INTEGER, updated_at INTEGER
    );

    CREATE TABLE IF NOT EXISTS calls (
      id TEXT PRIMARY KEY, client_id TEXT, from_number TEXT,
      status TEXT DEFAULT 'missed', duration INTEGER DEFAULT 0,
      timestamp INTEGER, stored_at INTEGER
    );

    CREATE TABLE IF NOT EXISTS webhook_events (
      id INTEGER PRIMARY KEY AUTOINCREMENT, client_id TEXT,
      event TEXT, payload TEXT, status TEXT DEFAULT 'pending',
      created_at INTEGER
    );
  `);
}

// --- Mensagens ---
function insertMessage(msg) {
  db.prepare(`INSERT OR REPLACE INTO messages (id, client_id, from_number, body, has_media, media_file, timestamp, stored_at) VALUES (?,?,?,?,?,?,?,?)`).run(
    msg.id, msg.client_id, msg.from, msg.body, msg.has_media ? 1 : 0, msg.media_file || null, msg.timestamp || Date.now(), Date.now()
  );
}

function getMessages(clientId, limit = 50, offset = 0, fromNumber = '') {
  let sql = 'SELECT * FROM messages WHERE 1=1';
  const params = [];
  if (clientId) { sql += ' AND client_id = ?'; params.push(clientId); }
  if (fromNumber) { sql += ' AND from_number = ?'; params.push(fromNumber); }
  sql += ' ORDER BY timestamp DESC LIMIT ? OFFSET ?';
  params.push(limit, offset);
  return db.prepare(sql).all(...params);
}

function countMessages(clientId) {
  if (clientId) return db.prepare('SELECT COUNT(*) as c FROM messages WHERE client_id = ?').get(clientId).c;
  return db.prepare('SELECT COUNT(*) as c FROM messages').get().c;
}

function deleteMessages(clientId) {
  if (clientId) db.prepare('DELETE FROM messages WHERE client_id = ?').run(clientId);
  else db.prepare('DELETE FROM messages').run();
}

function cleanupMessages(ttlMinutes = 60) {
  const cutoff = Date.now() - ttlMinutes * 60 * 1000;
  db.prepare('DELETE FROM messages WHERE stored_at < ?').run(cutoff);
}

function exportMessages(clientId) {
  let sql = 'SELECT * FROM messages';
  const params = [];
  if (clientId) { sql += ' WHERE client_id = ?'; params.push(clientId); }
  sql += ' ORDER BY timestamp ASC';
  return db.prepare(sql).all(...params);
}

// --- Estatisticas ---
function ensureStats(clientId) {
  db.prepare('INSERT OR IGNORE INTO stats (client_id, received, sent, media) VALUES (?,0,0,0)').run(clientId);
}

const ALLOWED_STAT_FIELDS = ['received', 'sent', 'media'];

function incrementStat(clientId, field) {
  if (!ALLOWED_STAT_FIELDS.includes(field)) return;
  ensureStats(clientId);
  db.prepare(`UPDATE stats SET ${field} = ${field} + 1 WHERE client_id = ?`).run(clientId);
}

function getStats(clientId) {
  if (clientId) return db.prepare('SELECT * FROM stats WHERE client_id = ?').get(clientId);
  return db.prepare('SELECT * FROM stats').all();
}

function getAllStats() {
  return db.prepare('SELECT client_id, SUM(received) as received, SUM(sent) as sent, SUM(media) as media FROM stats GROUP BY client_id').all();
}

function initStats(clientId) {
  db.prepare('INSERT OR IGNORE INTO stats (client_id, received, sent, media) VALUES (?,0,0,0)').run(clientId);
}

// --- Lista Negra ---
function addBlacklist(clientId, number) {
  db.prepare('INSERT OR IGNORE INTO blacklist (client_id, number) VALUES (?,?)').run(clientId, number);
}

function removeBlacklist(clientId, number) {
  db.prepare('DELETE FROM blacklist WHERE client_id = ? AND number = ?').run(clientId, number);
}

function getBlacklist(clientId) {
  if (clientId) return db.prepare('SELECT number FROM blacklist WHERE client_id = ?').all(clientId).map(r => r.number);
  return db.prepare('SELECT client_id, number FROM blacklist').all();
}

function isBlacklisted(clientId, number) {
  const r = db.prepare('SELECT 1 FROM blacklist WHERE client_id = ? AND number = ?').get(clientId, number);
  return !!r;
}

// --- Modo Humano ---
function enableHuman(clientId, number) {
  db.prepare('INSERT OR REPLACE INTO human_mode (client_id, number, since) VALUES (?,?,?)').run(clientId, number, Date.now());
}

function disableHuman(clientId, number) {
  db.prepare('DELETE FROM human_mode WHERE client_id = ? AND number = ?').run(clientId, number);
}

function isHuman(clientId, number) {
  const r = db.prepare('SELECT 1 FROM human_mode WHERE client_id = ? AND number = ?').get(clientId, number);
  return !!r;
}

function getHumanList(clientId) {
  return db.prepare('SELECT number, since FROM human_mode WHERE client_id = ?').all(clientId);
}

// --- Agendamentos ---
function listSchedules() {
  return db.prepare('SELECT * FROM schedules ORDER BY time ASC').all();
}

function addSchedule(s) {
  db.prepare('INSERT OR REPLACE INTO schedules (id, name, phone, message, time, days, client_id, enabled, created_at) VALUES (?,?,?,?,?,?,?,?,?)').run(
    s.id, s.name, s.phone, s.message, s.time, JSON.stringify(s.days || [0,1,2,3,4,5,6]), s.client_id || 'principal', s.enabled ? 1 : 0, s.created_at || new Date().toISOString()
  );
}

function deleteSchedule(id) {
  db.prepare('DELETE FROM schedules WHERE id = ?').run(id);
}

function toggleSchedule(id, enabled) {
  db.prepare('UPDATE schedules SET enabled = ? WHERE id = ?').run(enabled ? 1 : 0, id);
}

// --- Modelos ---
function listTemplates() {
  return db.prepare('SELECT * FROM templates').all();
}

function addTemplate(name, content) {
  db.prepare('INSERT OR REPLACE INTO templates (name, content, created_at) VALUES (?,?,?)').run(name, content, new Date().toISOString());
}

function deleteTemplate(name) {
  db.prepare('DELETE FROM templates WHERE name = ?').run(name);
}

// --- Armazenamento de Config ---
function setConfig(key, value) {
  db.prepare('INSERT OR REPLACE INTO config_store (key, value) VALUES (?,?)').run(key, JSON.stringify(value));
}

function getConfig(key) {
  const r = db.prepare('SELECT value FROM config_store WHERE key = ?').get(key);
  return r ? JSON.parse(r.value) : null;
}

// --- Labels ---
function addLabel(label) {
  db.prepare('INSERT OR REPLACE INTO labels (id, client_id, name, color, created_at) VALUES (?,?,?,?,?)').run(
    label.id, label.client_id, label.name, label.color || '#3b82f6', Date.now()
  );
}

function removeLabel(id) {
  db.prepare('DELETE FROM labels WHERE id = ?').run(id);
  db.prepare('DELETE FROM label_associations WHERE label_id = ?').run(id);
}

function getLabels(clientId) {
  if (clientId) return db.prepare('SELECT * FROM labels WHERE client_id = ?').all(clientId);
  return db.prepare('SELECT * FROM labels').all();
}

function associateLabel(labelId, targetId) {
  db.prepare('INSERT OR IGNORE INTO label_associations (label_id, target_id) VALUES (?,?)').run(labelId, targetId);
}

function removeLabelAssociation(labelId, targetId) {
  db.prepare('DELETE FROM label_associations WHERE label_id = ? AND target_id = ?').run(labelId, targetId);
}

function getLabelAssociations(labelId) {
  return db.prepare('SELECT target_id, target_type FROM label_associations WHERE label_id = ?').all(labelId);
}

// --- Contatos ---
function upsertContact(contact) {
  db.prepare(`INSERT OR REPLACE INTO contacts_cache (number, client_id, name, pushname, profile_pic, is_blocked, updated_at) VALUES (?,?,?,?,?,?,?)`).run(
    contact.number, contact.client_id, contact.name || null, contact.pushname || null,
    contact.profile_pic || null, contact.is_blocked ? 1 : 0, Date.now()
  );
}

function getContacts(clientId) {
  return db.prepare('SELECT * FROM contacts_cache WHERE client_id = ? ORDER BY name ASC').all(clientId);
}

function getContact(number, clientId) {
  return db.prepare('SELECT * FROM contacts_cache WHERE number = ? AND client_id = ?').get(number, clientId);
}

function deleteContact(number, clientId) {
  db.prepare('DELETE FROM contacts_cache WHERE number = ? AND client_id = ?').run(number, clientId);
}

// --- Grupos ---
function upsertGroup(group) {
  db.prepare(`INSERT OR REPLACE INTO groups_cache (id, client_id, name, description, participants, created_at, updated_at) VALUES (?,?,?,?,?,?,?)`).run(
    group.id, group.client_id, group.name, group.description || null, group.participants || 0,
    group.created_at || Date.now(), Date.now()
  );
}

function getGroups(clientId) {
  return db.prepare('SELECT * FROM groups_cache WHERE client_id = ? ORDER BY name ASC').all(clientId);
}

function deleteGroup(id) {
  db.prepare('DELETE FROM groups_cache WHERE id = ?').run(id);
}

// --- Chamadas ---
function insertCall(call) {
  db.prepare(`INSERT OR REPLACE INTO calls (id, client_id, from_number, status, duration, timestamp, stored_at) VALUES (?,?,?,?,?,?,?)`).run(
    call.id, call.client_id, call.from_number, call.status || 'missed', call.duration || 0,
    call.timestamp || Date.now(), Date.now()
  );
}

function getCalls(clientId, limit = 50) {
  if (clientId) return db.prepare('SELECT * FROM calls WHERE client_id = ? ORDER BY timestamp DESC LIMIT ?').all(clientId, limit);
  return db.prepare('SELECT * FROM calls ORDER BY timestamp DESC LIMIT ?').all(limit);
}

// --- Webhook Events Log ---
function logWebhookEvent(clientId, event, payload) {
  db.prepare('INSERT INTO webhook_events (client_id, event, payload, status, created_at) VALUES (?,?,?,?,?)').run(
    clientId, event, JSON.stringify(payload), 'sent', Date.now()
  );
}

function getWebhookEvents(clientId, limit = 50) {
  if (clientId) return db.prepare('SELECT * FROM webhook_events WHERE client_id = ? ORDER BY created_at DESC LIMIT ?').all(clientId, limit);
  return db.prepare('SELECT * FROM webhook_events ORDER BY created_at DESC LIMIT ?').all(limit);
}

function updateWebhookEventStatus(id, status, error = null) {
  db.prepare('UPDATE webhook_events SET status = ?, error = ? WHERE id = ?').run(status, error, id);
}

function getFailedWebhookEvents(clientId, limit = 50) {
  if (clientId) return db.prepare("SELECT * FROM webhook_events WHERE client_id = ? AND status = 'failed' ORDER BY created_at DESC LIMIT ?").all(clientId, limit);
  return db.prepare("SELECT * FROM webhook_events WHERE status = 'failed' ORDER BY created_at DESC LIMIT ?").all(limit);
}

function walCheckpoint() {
  try { if (db) db.pragma('wal_checkpoint(RESTART)'); } catch {}
}

function cleanupWebhookEvents(days = 30) {
  const cutoff = Date.now() - days * 24 * 60 * 60 * 1000;
  try { if (db) db.prepare('DELETE FROM webhook_events WHERE created_at < ?').run(cutoff); } catch {}
}

// ---
function reload() {
  if (db) try { db.close(); } catch {}
  DB_INITED = false;
  open();
}

function close() {
  if (db) try { db.close(); } catch {}
}

module.exports = {
  open, close, reload, get: () => db, inited: () => DB_INITED,
  insertMessage, getMessages, countMessages, deleteMessages, cleanupMessages, exportMessages,
  incrementStat, getStats, getAllStats, initStats,
  addBlacklist, removeBlacklist, getBlacklist, isBlacklisted,
  enableHuman, disableHuman, isHuman, getHumanList,
  listSchedules, addSchedule, deleteSchedule, toggleSchedule,
  listTemplates, addTemplate, deleteTemplate,
  setConfig, getConfig,
  addLabel, removeLabel, getLabels, associateLabel, removeLabelAssociation, getLabelAssociations,
  upsertContact, getContacts, getContact, deleteContact,
  upsertGroup, getGroups, deleteGroup,
  insertCall, getCalls,
  logWebhookEvent, getWebhookEvents, updateWebhookEventStatus, getFailedWebhookEvents, cleanupWebhookEvents, walCheckpoint,
};
