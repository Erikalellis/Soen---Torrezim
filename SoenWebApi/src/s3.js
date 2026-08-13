const Minio = require('minio');
const path = require('path');
const fs = require('fs');
const cfg = require('./config');
const core = require('./core');

let s3Client = null;
let BUCKET = 'zap-media';
let enabled = false;

function init() {
  const conf = cfg.get();
  const s3cfg = conf.s3 || {};
  if (!s3cfg.endpoint || !s3cfg.accessKey || !s3cfg.secretKey) {
    core.log('INFO', 'S3/MinIO nao configurado');
    return;
  }
  try {
    s3Client = new Minio.Client({
      endPoint: s3cfg.endpoint,
      port: parseInt(s3cfg.port) || 443,
      useSSL: s3cfg.useSSL !== false,
      accessKey: s3cfg.accessKey,
      secretKey: s3cfg.secretKey,
    });
    BUCKET = s3cfg.bucket || 'zap-media';
    enabled = true;
    core.log('INFO', `S3/MinIO configurado: ${s3cfg.endpoint}/${BUCKET}`);
  } catch (e) {
    core.log('ERROR', `S3 init: ${e.message}`);
  }
}

let bucketChecked = false;

async function uploadFile(localPath, remoteName) {
  if (!enabled || !s3Client) return null;
  try {
    if (!bucketChecked) {
      const exists = await s3Client.bucketExists(BUCKET);
      if (!exists) await s3Client.makeBucket(BUCKET);
      bucketChecked = true;
    }
    await s3Client.fPutObject(BUCKET, remoteName, localPath);
    core.log('INFO', `Arquivo enviado para S3: ${remoteName}`);
    return remoteName;
  } catch (e) {
    core.log('ERROR', `S3 upload: ${e.message}`);
    return null;
  }
}

async function downloadFile(remoteName, localPath) {
  if (!enabled || !s3Client) return false;
  try {
    await s3Client.fGetObject(BUCKET, remoteName, localPath);
    return true;
  } catch (e) {
    core.log('ERROR', `S3 download: ${e.message}`);
    return false;
  }
}

async function listFiles(prefix = '') {
  if (!enabled || !s3Client) return [];
  const objects = [];
  return new Promise((resolve, reject) => {
    const stream = s3Client.listObjects(BUCKET, prefix, true);
    stream.on('data', obj => objects.push(obj));
    stream.on('end', () => resolve(objects));
    stream.on('error', reject);
  });
}

async function deleteFile(remoteName) {
  if (!enabled || !s3Client) return false;
  try {
    await s3Client.removeObject(BUCKET, remoteName);
    return true;
  } catch (e) {
    core.log('ERROR', `S3 delete: ${e.message}`);
    return false;
  }
}

function getUrl(remoteName) {
  if (!enabled || !s3Client) return null;
  const s3cfg = cfg.get().s3 || {};
  const proto = s3cfg.useSSL !== false ? 'https' : 'http';
  return `${proto}://${s3cfg.endpoint}/${BUCKET}/${remoteName}`;
}

function isEnabled() { return enabled; }

module.exports = { init, uploadFile, downloadFile, listFiles, deleteFile, getUrl, isEnabled };
