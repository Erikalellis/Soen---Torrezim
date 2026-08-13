# DDS: ZAP-API v3

Bot inteligente para WhatsApp Web com multi-clientes, IA (OpenAI + Ollama),
agendamentos, WebSocket em tempo real, interface gráfica Python e painel web administrativo.

## Funcionalidades

- **Multi-clientes**: opere 1 ou vários números WhatsApp simultaneamente
- **Bot inteligente**: responde com base em intenções configuráveis + fallback para IA
- **IA dupla**: OpenAI (GPT-4o-mini) ou Ollama (local, gratuito)
- **Modo humano**: transfere conversas para atendentes reais com reset automático
- **Redirecionamento**: encaminha para setores específicos (financeiro, suporte, etc.)
- **Agendamentos**: envie mensagens programadas em dias/horários específicos
- **WebSocket**: eventos em tempo real (mensagens, QR Code, status, presença, chamadas, contatos, chats)
- **Webhook**: notifique sistemas externos com 19 tipos de evento + assinatura HMAC-SHA256
- **JWT**: autenticação por token de instância (além da API Key global)
- **Labels**: crie e associe labels a conversas/contatos
- **Grupos**: crie, gerencie participantes, promova/rebaixe admins
- **Contatos**: liste, busque, bloqueie/desbloqueie
- **Presença**: consulte status online/digitando (última vez online)
- **Chamadas**: receba e rejeite chamadas automaticamente
- **Blacklist**: bloqueie números indesejados
- **Modo férias**: mensagem automática de ausência
- **Modo administrador**: restrinja interação a um número controlador
- **3 modos de operação**: single, central (redirecionamento), multi
- **Mídia**: suporte a imagens, vídeos, áudios, documentos
- **Transcrição de áudio**: transcreva áudios via OpenAI Whisper
- **S3/MinIO**: armazenamento de mídia na nuvem
- **Message Queue**: publique eventos em RabbitMQ, Kafka ou Amazon SQS
- **Proxy**: suporte a proxy HTTP por instância
- **Estatísticas**: métricas de recebimento/envio por cliente
- **Log rotation**: server.log reciclado automaticamente aos 5MB
- **Painel web**: interface administrativa completa (15 páginas)
- **Interface Python**: GUI com 7 abas, WebSocket + polling
- **Docker**: imagem pronta para deploy (Node 22 + Chromium)
- **CI/CD**: GitHub Actions com testes em Node 18/20/22

## Requisitos

- **Node.js** 18+ (recomendado 22)
- **Chrome/Chromium** (auto-detectado ou via `CHROME_PATH`)
- **NPM**

## Instalação Rápida

```bash
# Clone o repositorio
git clone <seu-repo>
cd zap-api

# Instale dependencias
npm install

# Configure (opcional - ja vem com exemplos)
cp .env.example .env   # edite suas chaves

# Inicie
npm start
```

Acesse:
- Painel Admin: `http://localhost:3000/admin`
- Documentação interativa (Swagger): `http://localhost:3000/docs`
- API: `http://localhost:3000`

## Docker

```bash
docker compose up -d
```

A imagem instala Chromium automaticamente. Persistência via volumes:
`./data`, `./media`, `./sessions`.

## Testes

```bash
npm test                 # 36+ testes: config, core, database, server
npm run test:watch       # modo watch
```

Os testes usam `DB_PATH` separado para não interferir com o servidor em execução.

## Exemplos de Integração

Disponíveis em `docs/`:

| Arquivo | Descrição |
|---------|-----------|
| `docs/enviar-texto.sh` | Enviar texto via curl |
| `docs/enviar-texto.py` | Enviar texto via Python |
| `docs/enviar-texto.js` | Enviar texto via Node.js |
| `docs/websocket.py` | Escutar eventos WS em tempo real |
| `docs/scanner-qr.py` | Exibir QR Code automaticamente |

## Estrutura do Projeto

```
zap-api/
├── index.js              # Entry point
├── config.json           # Configuracao (intencoes, clientes, modos)
├── .env                  # Variaveis de ambiente (chaves, portas)
├── .env.example          # Template do .env (sem segredos)
├── swagger.json          # Documentacao OpenAPI 3.0 (~80 endpoints)
├── Dockerfile            # Imagem Docker (Node 22 + Chromium)
├── docker-compose.yml    # Orquestracao Docker
├── app.py                # Interface Grafica Python (7 abas)
├── docs/                 # Exemplos de integracao
├── test/                 # Testes automatizados (4 suites, 36+ testes)
├── src/
│   ├── config.js         # Carregamento de config.json + .env
│   ├── core.js           # Logger, validacao, webhook events (19 eventos)
│   ├── database.js       # SQLite (13 tabelas)
│   ├── whatsapp.js       # Clientes WhatsApp, IA, transcricao, eventos
│   ├── server.js         # Express + WebSocket + ~80 rotas + scheduler
│   ├── jwt.js            # Autenticacao JWT por instancia
│   ├── s3.js             # Armazenamento S3/MinIO
│   ├── transcribe.js     # Transcricao de audio (Whisper)
│   └── mq.js             # Message Queue (RabbitMQ, Kafka, SQS)
├── public/
│   └── index.html        # Painel web (15 paginas, SPA)
├── data/                 # Banco SQLite (criado automaticamente)
├── media/                # Arquivos de midia
└── sessions/             # Sessoes de autenticacao WhatsApp
```

## Configuração

### config.json

O arquivo `config.json` contém toda a configuração do bot. Exemplo completo:

```json
{
  "mode": "single",
  "port": 3000,
  "botName": "Minha Empresa",
  "botEnabled": true,
  "vacationMode": false,
  "vacationMessage": "Ola! Estamos em ferias...",
  "controllerNumber": "",
  "humanResetHours": 24,
  "messageTtlMinutes": 60,
  "rateLimit": { "windowMinutes": 15, "maxRequests": 100 },
  "clients": [
    {
      "id": "principal",
      "name": "Principal",
      "greetings": ["oi", "ola", "menu"],
      "greetingMessage": "Bem-vindo! Digite o numero da opcao:\n1- Atendimento\n2- Financeiro...",
      "fallbackMessage": "Nao entendi. Digite MENU.",
      "intents": [
        { "patterns": ["1", "atendimento"], "response": "Voce sera atendido...", "redirect": true },
        { "patterns": ["obrigado"], "response": "Por nada!", "redirect": false }
      ],
      "redirects": {
        "atendimento": { "phone": "5511999991111", "label": "Atendimento" }
      }
    }
  ]
}
```

### .env

| Variavel | Descrição |
|----------|-----------|
| `PORT` | Porta do servidor (3000) |
| `API_KEY` | Chave de API (vazio = sem auth) |
| `OPENAI_API_KEY` | Chave OpenAI para IA |
| `OPENAI_MODEL` | Modelo OpenAI (gpt-4o-mini) |
| `AI_PROVIDER` | openai ou ollama |
| `OLLAMA_URL` | URL do Ollama |
| `JWT_SECRET` | Segredo para tokens JWT |

## API - Endpoints Completos (~80)

### Sistema
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/ping` | Health check |
| GET | `/status` | Status completo do sistema |
| GET | `/qrcode` | QR Code de um cliente |
| GET | `/docs` | Swagger UI |
| GET | `/admin` | Painel web |
| GET | `/api/server/status` | Status do servidor (PID, uptime, memória) |
| POST | `/api/server/stop` | Parar o servidor |
| POST | `/api/server/restart` | Reiniciar o servidor |

### Autenticação
| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/auth/token` | Gerar token JWT |
| GET | `/api/auth/check` | Verificar token |

### Conexão
| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/client/:clientId/reconnect` | Reconectar cliente |

### Envio
| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/send/text` | Enviar texto |
| POST | `/api/send/media` | Enviar mídia (URL ou arquivo) |
| POST | `/api/send/poll` | Criar enquete |
| POST | `/api/send/ai` | Enviar com IA |
| POST | `/api/send/location` | Enviar localização |
| POST | `/api/send/contact` | Enviar contato (vCard) |
| POST | `/api/react` | Reagir a mensagem |
| POST | `/api/broadcast` | Broadcast para múltiplos números |

### Mensagens
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/messages` | Listar mensagens |
| GET | `/api/messages/export` | Exportar JSON |
| DELETE | `/api/messages` | Limpar histórico |
| GET | `/api/conversations` | Conversas agrupadas |

### Contatos
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/contacts` | Listar contatos |
| GET | `/api/contacts/:number` | Buscar contato |
| POST | `/api/contacts/block` | Bloquear |
| POST | `/api/contacts/unblock` | Desbloquear |

### Grupos
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/groups` | Listar grupos |
| GET | `/api/groups/:groupId` | Detalhes do grupo |
| POST | `/api/groups/create` | Criar grupo |
| POST | `/api/groups/add` | Adicionar participante |
| POST | `/api/groups/remove` | Remover participante |
| POST | `/api/groups/promote` | Promover admin |
| POST | `/api/groups/demote` | Rebaixar admin |
| POST | `/api/groups/leave` | Sair do grupo |
| POST | `/api/groups/setSubject` | Alterar nome |
| POST | `/api/groups/setDescription` | Alterar descrição |
| POST | `/api/groups/send` | Enviar mensagem |

### Labels
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/labels` | Listar labels |
| POST | `/api/labels` | Criar label |
| PUT | `/api/labels/:id` | Atualizar label |
| DELETE | `/api/labels/:id` | Excluir label |
| POST | `/api/labels/associate` | Associar label |
| DELETE | `/api/labels/associate` | Desassociar label |

### Presença e Chamadas
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/presence/:number` | Status de presença |
| POST | `/api/presence/sendPresence` | Enviar presença |
| GET | `/api/calls` | Listar chamadas |

### Configuração
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/config` | Ler configuração |
| PUT | `/api/config` | Salvar configuração |
| GET | `/api/config/ai` | Config de IA |
| PUT | `/api/config/ai` | Salvar IA |
| GET | `/api/config/webhook` | Ver webhook |
| POST | `/api/config/webhook` | Definir webhook |
| DELETE | `/api/config/webhook` | Remover webhook |
| GET | `/api/config/proxy` | Ver proxy |
| PUT | `/api/config/proxy` | Definir proxy |
| GET | `/api/config/mq` | Status MQ |
| POST | `/api/bot/toggle` | Ligar/desligar bot |
| POST | `/api/vacation` | Modo férias |
| GET | `/api/webhook/events` | Log de eventos |

### Moderação
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/blacklist` | Listar blacklist |
| POST | `/api/blacklist` | Bloquear número |
| DELETE | `/api/blacklist` | Desbloquear |
| GET | `/api/human` | Listar modo humano |
| POST | `/api/human` | Ativar/desativar modo humano |

### Ferramentas
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/templates` | Listar templates |
| POST | `/api/templates` | Criar template |
| DELETE | `/api/templates` | Excluir (body) |
| DELETE | `/api/templates/:name` | Excluir (path) |
| GET | `/api/schedules` | Listar agendamentos |
| POST | `/api/schedules` | Criar agendamento |
| DELETE | `/api/schedules` | Excluir (body) |
| PATCH | `/api/schedules` | Ativar/desativar |
| DELETE | `/api/schedules/:id` | Excluir (path) |
| POST | `/api/schedules/:id/toggle` | Toggle rápido |
| GET | `/api/log` | Visualizar log |
| DELETE | `/api/log` | Limpar log |

### Arquivos e Armazenamento
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/media` | Listar mídias |
| GET | `/api/media/:fileName` | Baixar mídia |
| POST | `/api/upload` | Upload (base64) |
| GET | `/api/backup` | Download DB |
| POST | `/api/backup/restore` | Restaurar DB |
| GET | `/api/s3/status` | Status S3 |
| GET | `/api/s3/files` | Listar S3 |

### Estatísticas
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/stats` | Estatísticas consolidadas |

## Autenticação

Se `API_KEY` estiver definida no `.env`, todas as rotas `/api/*` exigem:

```
x-api-key: sua-chave-aqui
```

Ou use JWT (token por instância, 30 dias de validade):

```
Authorization: Bearer <token-jwt>
```

Gere tokens via `POST /api/auth/token`.

## WebSocket

Conecte em `ws://localhost:3000/ws?token=SUA_API_KEY` (token obrigatório se `API_KEY` definida).

Formato das mensagens:

```json
{ "event": "message", "data": { ... }, "timestamp": 1700000000000 }
{ "event": "qr", "data": { "client_id": "principal", "qr": "data:image/png;base64,..." }, "timestamp": ... }
{ "event": "status", "data": { "client_id": "principal", "connected": true, "ready": true }, "timestamp": ... }
```

Eventos: `message`, `qr`, `status`, `presence`, `contacts.*`, `chats.*`, `call`

## Webhook Events (19 eventos)

A API dispara webhooks com assinatura HMAC-SHA256 (se `WEBHOOK_SECRET` configurado).
Header: `X-Webhook-Signature`.

| Evento | Disparo |
|--------|---------|
| `qrcode.updated` | QR Code atualizado |
| `connection.update` | Status da conexão mudou |
| `messages.upsert` | Mensagem recebida |
| `messages.update` | Mensagem atualizada (ack) |
| `send.message` | Mensagem enviada via API |
| `contacts.set` | Lista inicial de contatos carregada |
| `contacts.upsert` | Contato adicionado/atualizado |
| `contacts.update` | Contato atualizado |
| `presence.update` | Status de presença mudou |
| `chats.set` | Lista inicial de chats carregada |
| `chats.update` | Chat atualizado |
| `chats.upsert` | Novo chat |
| `chats.delete` | Chat deletado |
| `groups.upsert` | Grupo criado / membro entrou |
| `groups.update` | Grupo atualizado / membro saiu |
| `group-participants.update` | Participante add/remove/promote/demote |
| `call.upsert` | Chamada recebida |
| `labels.association` | Label associada |
| `labels.edit` | Label editada |

## Message Queue

Configure no `config.json` ou `.env` para publicar eventos em:

- **RabbitMQ**: exchange `zap-events`, routing key `zap.{event}`
- **Kafka**: tópico configurável, chave = nome do evento
- **Amazon SQS**: fila padrão com atributo `event`

## Modos de Operação

Configure em `config.json > mode`:

### Single (padrão)
Um número WhatsApp com bot respondendo automaticamente via intenções + IA.

### Central
Um número principal que recebe as mensagens e redireciona para atendentes
específicos com base em palavras-chave configuradas em `redirects`.

### Multi
Vários números independentes rodando simultaneamente, cada um com sua
própria sessão e configuração de intenções.

## Bot e Intenções

O fluxo de processamento de mensagens é:

1. Mensagem chega → filtra grupo/não-chat
2. Verifica modo férias → mensagem automática
3. Verifica botEnabled → silencia se desligado
4. Incrementa estatísticas
5. Verifica blacklist → silencia se bloqueado
6. Verifica modo humano → silencia se ativo
7. Verifica controlador → só admin interage
8. Salva mensagem no banco + transcrição + S3
9. Broadcast via WebSocket + Webhook
10. Verifica saudação → envia menu
11. Verifica intenções (word boundary regex) → resposta ou redirect
12. Fallback para IA (OpenAI/Ollama) → resposta inteligente
13. Se IA falhar → fallbackMessage configurada

## Interface Gráfica Python

```bash
pip install requests pillow qrcode[pil] websocket-client
python app.py
```

7 abas: Conexão, Enviar, Mensagens, Configuração, Ferramentas, Estatísticas, Navegador.

Conecta via WebSocket com fallback para HTTP polling.

## S3/MinIO

Configure em `config.json`:

```json
{
  "s3": {
    "endpoint": "s3.amazonaws.com",
    "port": 443,
    "useSSL": true,
    "accessKey": "sua-access-key",
    "secretKey": "sua-secret-key",
    "bucket": "zap-media"
  }
}
```

## CI/CD

GitHub Actions em `.github/workflows/ci.yml`:
- Node 18, 20, 22
- `npm test` + lint check
- Push/PR para `main`/`master`

## Log Rotation

O `server.log` é reciclado automaticamente ao atingir 5MB.
O arquivo antigo é renomeado para `server.log.YYYY-MM-DD`.

## Licença

MIT
