const amqplib = require('amqplib');
const { Kafka } = require('kafkajs');
const { SQSClient, SendMessageCommand } = require('@aws-sdk/client-sqs');
const cfg = require('./config');
const core = require('./core');

let rabbitConn = null;
let rabbitChannel = null;
let kafkaProducer = null;
let sqsClient = null;

async function initRabbitMQ(url) {
  try {
    rabbitConn = await amqplib.connect(url);
    rabbitChannel = await rabbitConn.createChannel();
    core.log('INFO', 'RabbitMQ conectado');
    return true;
  } catch (e) {
    core.log('WARN', `RabbitMQ: ${e.message}`);
    return false;
  }
}

async function initKafka(brokers) {
  try {
    const kafka = new Kafka({ clientId: 'zap-api', brokers });
    kafkaProducer = kafka.producer();
    await kafkaProducer.connect();
    core.log('INFO', 'Kafka conectado');
    return true;
  } catch (e) {
    core.log('WARN', `Kafka: ${e.message}`);
    return false;
  }
}

async function initSQS(config) {
  try {
    sqsClient = new SQSClient({
      region: config.region || 'us-east-1',
      credentials: { accessKeyId: config.accessKeyId, secretAccessKey: config.secretAccessKey },
    });
    core.log('INFO', 'SQS configurado');
    return true;
  } catch (e) {
    core.log('WARN', `SQS: ${e.message}`);
    return false;
  }
}

async function init() {
  const conf = cfg.get();
  const mq = conf.messageQueue || {};
  if (mq.rabbitmq) await initRabbitMQ(mq.rabbitmq);
  if (mq.kafka?.brokers) await initKafka(mq.kafka.brokers);
  if (mq.sqs?.accessKeyId) await initSQS(mq.sqs);
}

async function publishEvent(event, data) {
  const conf = cfg.get();
  const mq = conf.messageQueue || {};
  const payload = JSON.stringify({ event, data, timestamp: Date.now() });

  if (rabbitChannel && mq.rabbitmq) {
    try {
      const exchange = mq.rabbitExchange || 'zap-events';
      await rabbitChannel.assertExchange(exchange, 'topic', { durable: true });
      rabbitChannel.publish(exchange, `zap.${event}`, Buffer.from(payload));
    } catch (e) { core.log('WARN', `RabbitMQ publish: ${e.message}`); }
  }

  if (kafkaProducer && mq.kafka?.topic) {
    try {
      await kafkaProducer.send({
        topic: mq.kafka.topic,
        messages: [{ key: event, value: payload }],
      });
    } catch (e) { core.log('WARN', `Kafka publish: ${e.message}`); }
  }

  if (sqsClient && mq.sqs?.queueUrl) {
    try {
      await sqsClient.send(new SendMessageCommand({
        QueueUrl: mq.sqs.queueUrl,
        MessageBody: payload,
        MessageAttributes: { event: { DataType: 'String', StringValue: event } },
      }));
    } catch (e) { core.log('WARN', `SQS publish: ${e.message}`); }
  }
}

async function close() {
  if (rabbitChannel) try { await rabbitChannel.close(); } catch {}
  if (rabbitConn) try { await rabbitConn.close(); } catch {}
  if (kafkaProducer) try { await kafkaProducer.disconnect(); } catch {}
}

function isEnabled() { return !!(rabbitConn || kafkaProducer || sqsClient); }

module.exports = { init, publishEvent, close, isEnabled };
