const FormData = require('form-data');
const axios = require('axios');
const fs = require('fs');
const path = require('path');
const cfg = require('./config');
const core = require('./core');

async function transcribeAudio(audioPath) {
  const conf = cfg.get();
  const key = conf.openaiKey || process.env.OPENAI_API_KEY;
  if (!key) {
    core.log('WARN', 'Transcricao: OPENAI_API_KEY nao configurada');
    return null;
  }
  if (!fs.existsSync(audioPath)) {
    core.log('WARN', `Transcricao: arquivo nao encontrado ${audioPath}`);
    return null;
  }
  try {
    const form = new FormData();
    form.append('file', fs.createReadStream(audioPath));
    form.append('model', 'whisper-1');
    form.append('language', 'pt');

    const r = await axios.post('https://api.openai.com/v1/audio/transcriptions', form, {
      headers: { Authorization: `Bearer ${key}`, ...form.getHeaders() },
      timeout: 30000,
      maxContentLength: 25 * 1024 * 1024,
      maxBodyLength: 25 * 1024 * 1024,
    });
    const text = r.data.text?.trim();
    if (text) core.log('INFO', `Audio transcrito: "${text.substring(0, 80)}..."`);
    return text || null;
  } catch (e) {
    core.log('WARN', `Transcricao: ${e.message}`);
    return null;
  }
}

module.exports = { transcribeAudio };
