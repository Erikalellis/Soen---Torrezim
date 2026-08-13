const jwt = require('jsonwebtoken');

function resolveSecret() {
  const sec = process.env.JWT_SECRET;
  if (!sec || sec === 'zap-api-jwt-secret-change-me') return null;
  return sec;
}

function generateToken(instanceId, expiresIn = '30d') {
  const secret = resolveSecret();
  if (!secret) return null;
  return jwt.sign({ instance: instanceId, type: 'instance' }, secret, { expiresIn });
}

function verifyToken(token) {
  const secret = resolveSecret();
  if (!secret) return { valid: false, error: 'JWT_SECRET nao configurado' };
  try {
    const decoded = jwt.verify(token, secret);
    return { valid: true, instance: decoded.instance };
  } catch (e) {
    return { valid: false, error: e.message };
  }
}

module.exports = { generateToken, verifyToken };
