#!/usr/bin/env node
/** Exemplo: enviar mensagem de texto via API */
const API = process.env.API_URL || 'http://localhost:3000';
const KEY = process.env.API_KEY || '';

const num = process.argv[2] || '5511999999999';
const msg = process.argv[3] || 'Ola, tudo bem?';

const opts = {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ number: num, message: msg }),
};
if (KEY) opts.headers['x-api-key'] = KEY;

fetch(`${API}/api/send/text`, opts)
  .then(r => r.json())
  .then(console.log)
  .catch(console.error);
