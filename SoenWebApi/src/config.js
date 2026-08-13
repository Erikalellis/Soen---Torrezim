const path = require('path');
const fs = require('fs');
const crypto = require('crypto');

const BASE = path.join(__dirname, '..');
const CONFIG_PATH = path.join(BASE, 'config.json');
const ENV_PATH = path.join(BASE, '.env');

let CFG = {};

function randomSecret(bytes) {
  return crypto.randomBytes(bytes).toString('hex');
}

/**
 * Garante segredos fortes no .env. Quando API_KEY ou JWT_SECRET estao vazios ou
 * usando o valor placeholder, gera valores aleatorios e persiste no .env local.
 * Isso protege a API mesmo sendo servida apenas em 127.0.0.1.
 */
function garantirSegredos(env) {
  const defaultJwt = 'zap-api-jwt-secret-change-me';
  let mudou = false;
  if (!env.API_KEY || env.API_KEY.trim() === '') {
    env.API_KEY = 'ak_' + randomSecret(24);
    mudou = true;
  }
  if (!env.JWT_SECRET || env.JWT_SECRET.trim() === '' || env.JWT_SECRET.trim() === defaultJwt) {
    env.JWT_SECRET = randomSecret(32);
    mudou = true;
  }
if (mudou) {
        try {
          const linhas = fs.readFileSync(ENV_PATH, 'utf-8').split(/\r?\n/);
          const proc = (nome, valor) => {
            const regex = new RegExp('^#?\\s*' + nome + '=');
            const idx = linhas.findIndex((l) => regex.test(l));
            if (idx >= 0) linhas[idx] = nome + '=' + valor;
            else linhas.push(nome + '=' + valor);
          };
          proc('API_KEY', env.API_KEY);
          proc('JWT_SECRET', env.JWT_SECRET);
          fs.writeFileSync(ENV_PATH, linhas.join('\n'), 'utf-8');
        } catch {}
      }
      return env;
    }

function load() {
  try {
    CFG = JSON.parse(fs.readFileSync(CONFIG_PATH, 'utf-8'));
  } catch {
    CFG = { mode: 'single', port: 3000, clients: [] };
  }
  const env = {};
  try {
    const texto = fs.readFileSync(ENV_PATH, 'utf-8');
    for (const line of texto.split('\n')) {
      const m = line.trim().match(/^([^=]+)=(.*)$/);
      if (m) env[m[1].trim()] = m[2].trim().replace(/["']/g, '');
    }
  } catch {}
  garantirSegredos(env);
  for (const k of Object.keys(env)) process.env[k] = env[k];
  const E = process.env;
  if (E.PORT) CFG.port = parseInt(E.PORT);
  if (E.API_KEY) CFG.apiKey = E.API_KEY;
  if (E.BOT_NAME) CFG.botName = E.BOT_NAME;
  if (E.CHROME_PATH) CFG.chromiumPath = E.CHROME_PATH;
  if (E.OPENAI_API_KEY) CFG.openaiKey = E.OPENAI_API_KEY;
  if (E.OPENAI_MODEL) CFG.openaiModel = E.OPENAI_MODEL;
  if (E.AI_PROVIDER) CFG.aiProvider = E.AI_PROVIDER;
  if (E.OLLAMA_URL) CFG.ollamaUrl = E.OLLAMA_URL;
  if (E.OLLAMA_MODEL) CFG.ollamaModel = E.OLLAMA_MODEL;
  return CFG;
}

function get() { return CFG; }

function save(newCfg) {
  CFG = newCfg;
  fs.writeFileSync(CONFIG_PATH, JSON.stringify(newCfg, null, 2), 'utf-8');
}

module.exports = { load, get, save, CONFIG_PATH };
